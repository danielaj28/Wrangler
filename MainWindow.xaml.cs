using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wrangler
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		List<Device> devices = new List<Device>();
		List<Preset> presets = new List<Preset>();
		public MainWindow()
		{
			InitializeComponent();

			cbxSources.ItemsSource = devices;

			UpdateDriveList();

			presets.Add(new Preset { name = "ACamOliver" });
			presets.Add(new Preset { name = "BCamMalcom" });
			presets.Add(new Preset { name = "CCamDan" });
			presets.Add(new Preset { name = "DCamGopro" });
			presets.Add(new Preset { name = "ECamGoPro" });
			presets.Add(new Preset { name = "FCamSpare" });
			cbxPreset.ItemsSource = presets;
		}

		private void UpdateDriveList()
		{
			devices.Clear();
			DriveInfo[] drives = DriveInfo.GetDrives();

			foreach (DriveInfo drive in drives)
			{
				if (!drive.DriveType.Equals(DriveType.Network)){
					devices.Add(new Device { 
						driveLetter = drive.Name, 
						name = drive.VolumeLabel,
						used = (drive.TotalSize-drive.TotalFreeSpace)/1024 /1024,
						total=drive.TotalSize/1024/1024 
					});
				}
			}
			cbxSources.Items.Refresh();
		}

		private void btnDeviceRefresh_Click(object sender, RoutedEventArgs e)
		{
			UpdateDriveList();
		}
	}
}
