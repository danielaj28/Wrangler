using System;
using System.Windows;

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
		}

		private void btnDeletePreset_Click(object sender, RoutedEventArgs e)
		{

			MainWindow.presets.Remove((Preset)listPresets2.SelectedItem);

			listPresets2.Items.Refresh();

			mainWindow.UpdatePresets();
		}

	}
}
