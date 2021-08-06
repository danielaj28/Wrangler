using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wrangler
{
	/// <summary>
	/// Interaction logic for Presets.xaml
	/// </summary>
	public partial class Presets : Window
	{
		MainWindow mainWindow;
		public Presets(MainWindow mw)
		{
			InitializeComponent();

			listPresets2.ItemsSource = MainWindow.presets;
			mainWindow = mw;

		}

		private void btnAddPreset_Click(object sender, RoutedEventArgs e)
		{

			if (txtNewPreset.Text.Trim().Length>0)
			{
				MainWindow.presets.Add(new Preset { name = txtNewPreset.Text.Trim() });
				txtNewPreset.Clear();
				listPresets2.Items.Refresh();
				listPresets2.SelectedItem = MainWindow.presets[MainWindow.presets.Count - 1];

				mainWindow.UpdatePresets();
			}
			
			//listPresets2.SelectedItem = MainWindow.presets.get;
		}
	}
}
