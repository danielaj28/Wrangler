using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Wrangler
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static List<Device> devices = new List<Device>();
		public static List<Preset> presets = new List<Preset>();
		public static int totalFiles = 0;
		public static int processedFiles = 0;

		public MainWindow()
		{
			InitializeComponent();

			cbxSources.ItemsSource = devices;

			UpdateDriveList();

			var p = new Preset
			{
				name = "Test"
			};



			presets = DeSerializeObject<List<Preset>>("presets.xml");
			if (presets == null)
			{
				presets = new List<Preset>();

			}

			cbxPreset.ItemsSource = presets;

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
				//Log exception here
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
				//Log exception here
			}

			return objectOut;
		}

		private void UpdateDriveList()
		{
			devices.Clear();
			DriveInfo[] drives = DriveInfo.GetDrives();

			foreach (DriveInfo drive in drives)
			{
				if (!drive.DriveType.Equals(DriveType.Network) && !drive.DriveType.Equals(DriveType.Fixed))
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
		}

		private void btnDeviceRefresh_Click(object sender, RoutedEventArgs e)
		{
			UpdateDriveList();
		}

		private void btnStart_Click(object sender, RoutedEventArgs e)
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

			sourceFilePaths.AddRange(GetAllFiles(sourceDevice.driveLetter));

			List<Thread> threads = new List<Thread>();

			//Copy
			processedFiles = 0;
			totalFiles = sourceFilePaths.Count * targetPreset.paths.Count;
			pbr1.Maximum = totalFiles;
			txtProgress.Text = string.Format("0% {0}/{1} copied", processedFiles, totalFiles);
			btnStart.IsEnabled = false;

			foreach (var destinationPath in targetPreset.paths)
			{
				Thread t = new Thread(() => Copy(sourceFilePaths, destinationPath, this));
				t.Start();
				threads.Add(t);
			}

			//Verify


		}

		private void Copy(List<string> sourceFilePaths, string destinationPath, MainWindow mw)
		{
			foreach (string sourceFilePath in sourceFilePaths)
			{
				string relativeFilePath = sourceFilePath.Substring(sourceFilePath.IndexOf("\\", 0), sourceFilePath.Length - sourceFilePath.IndexOf("\\", 0));
				string middlePath = relativeFilePath.Substring(0, relativeFilePath.LastIndexOf("\\"));
				string directoryPathToCreate = destinationPath + middlePath;
				string filePathDestination = destinationPath + relativeFilePath;

				Directory.CreateDirectory(directoryPathToCreate);
				File.Copy(sourceFilePath, filePathDestination);
				IncrementProgress(mw);
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
			SerializeObject<List<Preset>>(presets, "presets.xml");
		}

		public void IncrementProgress(MainWindow mw)
		{
			lock (mw)
			{
				processedFiles++;
				Dispatcher.Invoke(() =>
				{
					mw.pbr1.Value = processedFiles;

					decimal percentage = ((decimal)processedFiles / (decimal)totalFiles) * 100;
					percentage = Math.Round(percentage);

					mw.txtProgress.Text = string.Format("{0}% {1}/{2} copied", percentage, processedFiles, totalFiles);

					if (totalFiles == processedFiles)
					{
						btnStart.IsEnabled = true;
					}
				});
			}
		}
	}
}
