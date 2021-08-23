using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using Standart.Hash.xxHash;
using System.Windows.Media;

namespace Wrangler
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static List<Device> devices = new List<Device>();
		public static List<Preset> presets = new List<Preset>();
		public static Dictionary<string, string> hashCache = new Dictionary<string, string>();
		public static int totalFiles = 0;
		public static int processedFiles = 0;
		public static int verifiedFiles = 0;
		public static Queue<Tuple<string, string>> verificationQueue = new Queue<Tuple<string, string>>();
		public static List<Thread> threads = new List<Thread>();
		public static Boolean running = true;

		public MainWindow()
		{
			try
			{
				InitializeComponent();

				cbxSources.ItemsSource = devices;

				UpdateDriveList();

				var p = new Preset
				{
					name = "Test"
				};

				try
				{
					presets = DeSerializeObject<List<Preset>>("presets.xml");
				}
				catch (FileNotFoundException)
				{

				}
				catch (Exception ex)
				{
					MessageBox.Show(String.Format("Unable to load presets from file: {0}", ex.Message), "Unable to load presets", MessageBoxButton.OK, MessageBoxImage.Error);
				}

				if (presets == null)
				{
					presets = new List<Preset>();
				}

				cbxPreset.ItemsSource = presets;

				Thread tv = new Thread(() => Verification(this));
				tv.Start();
				threads.Add(tv);
			}
			catch (Exception ex2)
			{
				MessageBox.Show(String.Format("There was an unexpected error: {0}", ex2.Message), "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
				throw;
			}
		}

		/// <summary>
		/// Serializes an object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="serializableObject"></param>
		/// <param name="fileName"></param>
		public void SerializeObject<T>(T serializableObject, string fileName)
		{
			if (serializableObject == null) { return; }

			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
				using (MemoryStream stream = new MemoryStream())
				{
					serializer.Serialize(stream, serializableObject);
					stream.Position = 0;
					xmlDocument.Load(stream);
					xmlDocument.Save(fileName);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		/// <summary>
		/// Deserializes an xml file into an object list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public T DeSerializeObject<T>(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) { return default(T); }

			T objectOut = default(T);

			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(fileName);
				string xmlString = xmlDocument.OuterXml;

				using (StringReader read = new StringReader(xmlString))
				{
					Type outType = typeof(T);

					XmlSerializer serializer = new XmlSerializer(outType);
					using (XmlReader reader = new XmlTextReader(read))
					{
						objectOut = (T)serializer.Deserialize(reader);
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}

			return objectOut;
		}

		private void UpdateDriveList()
		{
			devices.Clear();
			DriveInfo[] drives = DriveInfo.GetDrives();

			foreach (DriveInfo drive in drives)
			{
				if (!drive.DriveType.Equals(DriveType.Network) && !drive.DriveType.Equals(DriveType.Fixed) && drive.IsReady)
				{
					devices.Add(new Device
					{
						driveLetter = drive.Name,
						name = drive.VolumeLabel,
						used = (drive.TotalSize - drive.TotalFreeSpace) / 1024 / 1024,
						total = drive.TotalSize / 1024 / 1024
					});
				}
			}
			cbxSources.Items.Refresh();
			if (cbxSources.Items.Count > 0)
			{
				cbxSources.SelectedIndex = 0;
			}
		}

		private void btnDeviceRefresh_Click(object sender, RoutedEventArgs e)
		{
			UpdateDriveList();
		}

		private void btnStart_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Device sourceDevice = (Device)cbxSources.SelectedItem;

				if (sourceDevice == null)
				{
					MessageBox.Show("Need to select a removable drive to copy from.");
					return;
				}

				Preset targetPreset = (Preset)cbxPreset.SelectedItem;

				if (targetPreset == null || targetPreset.paths.Count == 0)
				{
					MessageBox.Show("Need to select a preset to copy too.");
					return;
				}

				List<string> sourceFilePaths = new List<string>();

				try
				{
					sourceFilePaths.AddRange(GetAllFiles(sourceDevice.driveLetter));
				}
				catch (Exception inner)
				{
					UpdateDriveList();
					throw new Exception("Unable to get list of files from source device", inner);
				}

				//Copy
				totalFiles = sourceFilePaths.Count * targetPreset.paths.Count;

				processedFiles = 0;
				pbr1.Maximum = totalFiles;
				pbr1.Foreground = Brushes.Yellow;
				pbr1.Value = processedFiles;
				txtProgress.Text = string.Format("0% {0}/{1} copied", processedFiles, totalFiles);

				verifiedFiles = 0;
				pbrVerified.Maximum = totalFiles;
				pbrVerified.Foreground = Brushes.Yellow;
				pbrVerified.Value = verifiedFiles;
				txtVerificationProgress.Text = string.Format("0% {0}/{1} verified", verifiedFiles, totalFiles);

				verificationQueue.Clear();

				btnStart.IsEnabled = false;

				foreach (var destinationPath in targetPreset.paths)
				{
					Thread t = new Thread(() => Copy(sourceFilePaths, destinationPath, this));
					t.Start();
					threads.Add(t);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occured");
				btnStart.IsEnabled = true;
			}
		}

		private void Copy(List<string> sourceFilePaths, string destinationPath, MainWindow mw)
		{
			try
			{
				foreach (string sourceFilePath in sourceFilePaths)
				{
					if (!running)
					{
						break;
					}
					string relativeFilePath = sourceFilePath.Substring(sourceFilePath.IndexOf("\\", 0), sourceFilePath.Length - sourceFilePath.IndexOf("\\", 0));
					string middlePath = relativeFilePath.Substring(0, relativeFilePath.LastIndexOf("\\"));
					string directoryPathToCreate = destinationPath + middlePath;
					string filePathDestination = destinationPath + relativeFilePath;

					Directory.CreateDirectory(directoryPathToCreate);
					File.Copy(sourceFilePath, filePathDestination);
					IncrementProgress(mw, "copy", String.Format("Copied {0} to {1}", sourceFilePath, filePathDestination));
					verificationQueue.Enqueue(new Tuple<string, string>(sourceFilePath, filePathDestination));
				}
			}
			catch (Exception ex)
			{
				IncrementProgress(mw, "copy-error", String.Format("Copy error {0}", ex));
				MessageBox.Show(ex.Message, String.Format("Error whilst copying to {0}", destinationPath));
			}
		}

		private void Verification(MainWindow mw)
		{
			try
			{
				Boolean waiting = true;

				while (running)
				{

					while (waiting)
					{
						if (verificationQueue.Count < totalFiles || totalFiles == 0)
						{
							Thread.Sleep(1000);
						}
						else
						{
							waiting = false;
						}
					}

					Tuple<string, string> hashCheck;
					try
					{
						hashCheck = verificationQueue.Dequeue();
					}
					catch (InvalidOperationException)
					{
						hashCheck = null;
					}

					if (hashCheck == null)
					{
						Thread.Sleep(100);
					}
					else
					{
						int retryAttempt = 4;
						while (retryAttempt > 0)
						{
							string sourceFilePath = "";
							string filePathDestination = "";

							try
							{
								sourceFilePath = hashCheck.Item1;
								filePathDestination = hashCheck.Item2;

								string sourceHash;
								string destinationHash;

								if (hashCache.ContainsKey(sourceFilePath))
								{
									lock (hashCache)
									{
										sourceHash = hashCache[sourceFilePath];
									}
								}
								else
								{
									using (var stream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
									{
										sourceHash = xxHash64.ComputeHash(stream).ToString();
										hashCache[sourceFilePath] = sourceHash;
									}
								}

								using (var stream = new FileStream(filePathDestination, FileMode.Open, FileAccess.Read))
								{
									destinationHash = xxHash64.ComputeHash(stream).ToString();
								}

								if (sourceHash == destinationHash)
								{
									IncrementProgress(mw, "verify", String.Format("Verified {0} with hash {1}", filePathDestination, destinationHash));
								}
								break;
							}
							catch (Exception ex)
							{
								Thread.Sleep(500);
								retryAttempt--;

								if (retryAttempt == 0)
								{
									Dispatcher.Invoke(() =>
									{
										mw.pbrVerified.Foreground = Brushes.Red;
										mw.btnStart.IsEnabled = true;
									});
									MessageBox.Show(String.Format("Unable to verify file {0} -> {1} Error: {2}", sourceFilePath, filePathDestination, ex.Message), "Unable to verify file", MessageBoxButton.OK, MessageBoxImage.Error);
								}
							}
						}
					}

					waiting = (verificationQueue.Count == 0);
				}
			}
			catch (ThreadInterruptedException)
			{

			}
			catch (Exception ex3)
			{
				Dispatcher.Invoke(() =>
				{
					mw.pbrVerified.Foreground = Brushes.Red;
					btnStart.IsEnabled = true;
				});
				MessageBox.Show(String.Format("Issue during verification:{0}", ex3.Message), "Unable to verify files", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private IEnumerable<string> GetAllFiles(string dir)
		{
			return Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
		}

		private void btnManagePresets_Click(object sender, RoutedEventArgs e)
		{
			Presets winPresets = new Presets(this);
			winPresets.Show();
			winPresets.Focus();
		}

		public void UpdatePresets()
		{
			cbxPreset.Items.Refresh();
			try
			{
				SerializeObject<List<Preset>>(presets, "presets.xml");
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("Unable to save presets to file: {0}", ex.Message), "Unable to save presets", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void IncrementProgress(MainWindow mw, string type, string lastEvent)
		{
			try
			{
				lock (mw)
				{
					Dispatcher.Invoke(() =>
					{
						mw.txtLog.Text = String.Format("{0}{1}{2}", lastEvent, Environment.NewLine, mw.txtLog.Text);
					});

					switch (type)
					{
						case "copy":
							processedFiles++;
							Dispatcher.Invoke(() =>
							{
								mw.pbr1.Value = processedFiles;

								decimal percentage = ((decimal)processedFiles / (decimal)totalFiles) * 100;
								percentage = Math.Round(percentage);

								mw.txtProgress.Text = string.Format("{0}% {1}/{2} copied", percentage, processedFiles, totalFiles);

								if (processedFiles == totalFiles)
								{
									mw.pbr1.Foreground = Brushes.Green;
								}
							});
							break;
						case "copy-error":
							Dispatcher.Invoke(() =>
							{
								mw.pbr1.Foreground = Brushes.Red;
							});
							break;
						case "verify":
							verifiedFiles++;
							Dispatcher.Invoke(() =>
							{
								mw.pbrVerified.Value = verifiedFiles;

								decimal percentage = ((decimal)verifiedFiles / (decimal)totalFiles) * 100;
								percentage = Math.Round(percentage);

								mw.txtVerificationProgress.Text = string.Format("{0}% {1}/{2} verified", percentage, verifiedFiles, totalFiles);

								if (totalFiles == verifiedFiles)
								{
									btnStart.IsEnabled = true;
									mw.pbrVerified.Foreground = Brushes.Green;
									MessageBox.Show("Copy and Verification Successfully Completed", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
								}
							});
							break;
						case "finish":
							Dispatcher.Invoke(() =>
							{
								mw.btnStart.IsEnabled = true;
							});
							break;
					}

				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("Unable to update progress: {0}", ex.Message), "Unable to update progress", MessageBoxButton.OK, MessageBoxImage.Error);
				btnStart.IsEnabled = true;
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			foreach (var thread in threads)
			{
				running = false;
				thread.Interrupt();
			}
		}
	}
}
