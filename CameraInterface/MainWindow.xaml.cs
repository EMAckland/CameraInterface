using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using EOSDigital.API;
using EOSDigital.SDK;

namespace CameraInterface
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		int LVBw, LVBh, w, h, ErrCount;
		float LVBratio, LVration;
		object ErrLock = new object();
		object LVLock = new object();

		private ICameraObserver Camera;
		private bool IsLiveViewOn = false;
		ImageBrush bgbrush = new ImageBrush();
		Action<BitmapImage> SetImageAction;
		System.Windows.Forms.FolderBrowserDialog SaveFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();

		public MainWindow()
		{
			Camera = new CameraController();
			ErrorHandler.SevereErrorHappened += ErrorHandler_SevereErrorHappened;
			ErrorHandler.NonSevereErrorHappened += ErrorHandler_NonSevereErrorHappened;

		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			SavePathTextBox.Text =
				System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "RemotePhoto");
			SetImageAction = (BitmapImage img) => { bgbrush.ImageSource = img; };
			SaveFolderBrowser.Description = "Save Images To...";
			Camera.SetGUI(this);
			Camera.SetInitGUI();
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			try
			{
				Camera?.CloseSession();
			}
			catch (Exception ex)
			{
				ReportError(ex.Message, false);
			}
		}

		private void CaptureButton_Click(object sender, EventArgs e)
		{
			Camera.TakePhoto(this, e);
		}

		private void FocusInButton_Click(object sender, EventArgs e)
		{
			Camera.FocusChanged(this, e);
		}

		public void FocusOutButton_Click(object sender, EventArgs e)
		{
			Camera.FocusChanged(this, e);
		}

		public void AvComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			Dispatcher.Invoke((Action) delegate
			{
				try
				{
					if (AvComboBox.SelectedIndex < 0 || !Camera.SessionOpen()) return;
					Camera.AvChanged(this, AvComboBox.SelectedIndex);
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
					ReportError(exception.Message, false);
				}
			});
		}

		public void TvComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			Dispatcher.Invoke((Action) delegate
			{
				try
				{
					if (TvComboBox.SelectedIndex < 0 || !Camera.SessionOpen()) return;
					Camera.TvChanged(this, TvComboBox.SelectedIndex);
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
					ReportError(exception.Message, false);
				}
			});
		}

		public void ISOComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			Dispatcher.Invoke((Action) delegate
			{
				try
				{
					if (ISOComboBox.SelectedIndex < 0 || !Camera.SessionOpen()) return;
					Camera.ISOChanged(this, ISOComboBox.SelectedIndex);
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
					ReportError(exception.Message, false);
				}
			});
		}

		private void BrowseButton_Click(object sender, EventArgs e)
		{
			try
			{
				if (Directory.Exists(SavePathTextBox.Text)) SaveFolderBrowser.SelectedPath = SavePathTextBox.Text;
				if (SaveFolderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					SavePathTextBox.Text = SaveFolderBrowser.SelectedPath;
					Camera.ChangeSaveDirectory(SaveFolderBrowser.SelectedPath);
				}
			}
			catch (Exception ex)
			{
				ReportError(ex.Message, false);
			}
		}

		#region Camera Events

		public void Camera_OpenedSession(Camera Cam)
		{
			SettingsGroupBox.IsEnabled = true;
			LiveViewGroupBox.IsEnabled = true;
			LVCanvas.IsEnabled = true;
			TakePhotoButton.IsEnabled = true;
			BrowseButton.IsEnabled = true;
		}

		public void Camera_ReportError(string message, bool lockdown)
		{
			ReportError(message, lockdown);
		}

		public void Camera_ProgressChanged(object sender, int progress)
		{
			try
			{
				MainProgressBar.Dispatcher.Invoke((Action) delegate { MainProgressBar.Value = progress; });
			}
			catch (Exception ex)
			{
				ReportError(ex.Message, false);
			}
		}

		public void Camera_StatusUpdate(String status)
		{
			Dispatcher.Invoke((Action) delegate { StatusListBox.Items.Add(status); });
		}

		public void Camera_DownloadReady(Camera sender, DownloadInfo Info)
		{
			try
			{
				string dir = null;
				SavePathTextBox.Dispatcher.Invoke((Action) delegate { dir = SavePathTextBox.Text; });
				sender.DownloadFile(Info, dir);
				MainProgressBar.Dispatcher.Invoke((Action) delegate { MainProgressBar.Value = 0; });
			}
			catch (Exception ex)
			{
				ReportError(ex.Message, false);
			}
		}

		public void ErrorHandler_NonSevereErrorHappened(object sender, ErrorCode ex)
		{
			ReportError($"SDK Error code: {ex} ({((int) ex).ToString("X")})", false);
		}

		public void ErrorHandler_SevereErrorHappened(object sender, Exception ex)
		{
			ReportError(ex.Message, true);
		}

		#endregion

		#region Live View

		public void Camera_LiveViewUpdated(Camera sender, Stream img)
		{
			try
			{
				using (WrapStream s = new WrapStream(img))
				{
					img.Position = 0;
					BitmapImage EvfImage = new BitmapImage();
					EvfImage.BeginInit();
					EvfImage.StreamSource = s;
					EvfImage.CacheOption = BitmapCacheOption.OnLoad;
					EvfImage.EndInit();
					EvfImage.Freeze();
					Dispatcher.BeginInvoke(SetImageAction, EvfImage);
				}
			}
			catch (Exception ex)
			{
				ReportError(ex.Message, false);
			}
		}

		public void LiveViewButton_Click(object sender, RoutedEventArgs e)
		{
			LVButton.Content = FindResource(LVButton.Content == FindResource("Play") ? "Stop" : "Play");
			try
			{
				Camera.LiveViewOnOff(sender, e);
				if (!IsLiveViewOn)
				{
					LVCanvas.Background = bgbrush;
				}
				else
				{
					LVCanvas.Background = Brushes.LightGray;
				}
			}
			catch (Exception ex)
			{
				ReportError(ex.Message, false);
			}
		}

		#endregion

		#region Subroutines

		public TextBox GetSavePathTextBox()
		{
			return SavePathTextBox;
		}

		public ComboBox GetComboBox(PropertyID id)
		{
			switch (id)
			{
				case PropertyID.Av: return AvComboBox;
				case PropertyID.Tv: return TvComboBox;
				case PropertyID.ISO: return ISOComboBox;
				default: return null;
			}
		}

		private void EnableUI(bool enable)
		{
			if (!Dispatcher.CheckAccess()) Dispatcher.Invoke((Action) delegate { EnableUI(enable); });
			else
			{
				SettingsGroupBox.IsEnabled = enable;
				StatusListBox.IsEnabled = enable;
				LiveViewGroupBox.IsEnabled = enable;
				BrowseButton.IsEnabled = enable;
			}
		}

		private void ReportError(string message, bool lockdown)
		{
			int errc;
			lock (ErrLock)
			{
				errc = ++ErrCount;
			}

			if (lockdown) EnableUI(false);

			if (errc < 4) MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			else if (errc == 4) MessageBox.Show("Many errors happened!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

			lock (ErrLock)
			{
				ErrCount--;
			}
		}
#endregion
	}

	
}
