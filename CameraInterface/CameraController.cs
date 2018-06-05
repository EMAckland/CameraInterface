using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CameraInterface.Properties;
using EOSDigital.API;
using EOSDigital.SDK;
using ComboBox = System.Windows.Controls.ComboBox;


namespace CameraInterface
{
	public class CameraController : ICameraObserver
	{
		public MainWindow GUI;
		CanonAPI APIHandler;
		Camera Camera;
		CameraValue[] AvList;
		CameraValue[] TvList;
		CameraValue[] ISOList;
		private bool IsInit, IsGUIInit = false;
		public bool IsConnected { get; set; }
		private ComboBox AvComboBox, TvComboBox, ISOComboBox;
		private TextBox SavePTextBox;
		private string saveDirectory = null;
		List<Camera> CamList;
		private string defSaveDir;

		public CameraController()
		{
			IsConnected = false;
			IsInit = false;
		}

		public Window GetCameraWindow()
		{
			return GUI;
		}

		public void SetGUI(Window CameraWindow)
		{
			GUI = (MainWindow)CameraWindow;
		}

		public void Init()
		{
			APIHandler = new CanonAPI();
			APIHandler.CameraAdded += APIHandler_CameraAdded;
			ErrorHandler.SevereErrorHappened += GUI.ErrorHandler_SevereErrorHappened;
			ErrorHandler.NonSevereErrorHappened += GUI.ErrorHandler_NonSevereErrorHappened;
			defSaveDir = @"c:"+ Resources.Resources.Strings.CAMERA_DIRECTORY + DateTime.Today.ToLocalTime().ToString("yy-MM-dd");
			Console.WriteLine(defSaveDir);
			RefreshCamera();
			OpenSession();
			SetInitGUI();
			IsInit = true;
		}
		public bool SessionOpen()
		{
			bool open = false;
			if (Camera == null) {
				return false;
			}
			return Camera.SessionOpen && IsConnected;
		}

		public void CloseSession()
		{
			IsInit = false;
			Camera?.CloseSession();
		}

		private void OpenSession()
		{
			if (CamList?.Count >= 1) {
				Camera = CamList[0];
				Camera.OpenSession();
				Camera.LiveViewUpdated += GUI.Camera_LiveViewUpdated;
				Camera.ProgressChanged += GUI.Camera_ProgressChanged;
				Camera.StateChanged += Camera_StateChanged;
				Camera.DownloadReady += Camera_DownloadReady;
				AvList = Camera.GetSettingsList(PropertyID.Av);
				TvList = Camera.GetSettingsList(PropertyID.Tv);
				ISOList = Camera.GetSettingsList(PropertyID.ISO);
				Camera.SetSetting(PropertyID.SaveTo, (int)SaveTo.Host);
				Camera.SetCapacity(4096, int.MaxValue);
				SetInitGUI();
			}
		}
		private void RefreshCamera()
		{
			CamList = APIHandler.GetCameraList();
		}

		public void SetInitGUI()
		{
			if (IsGUIInit || !GUI.IsInitialized) return;
			if (CamList?.Count >= 1 ) {
				try {
					GUI.Dispatcher.Invoke((Action)delegate {
						AvComboBox = GUI.GetComboBox(PropertyID.Av);
						TvComboBox = GUI.GetComboBox(PropertyID.Tv);
						ISOComboBox = GUI.GetComboBox(PropertyID.ISO);
						GUI.GetSavePathTextBox().Text = defSaveDir;

						foreach (var Av in AvList)
							AvComboBox.Items.Add(AvComboBox.Items.Add(Av.StringValue));
						foreach (var Tv in TvList)
							TvComboBox.Items.Add(TvComboBox.Items.Add(Tv.StringValue));
						foreach (var ISO in ISOList)
							ISOComboBox.Items.Add(ISOComboBox.Items.Add(ISO.StringValue));

						AvComboBox.SelectedIndex =
							AvComboBox.Items.IndexOf(AvValues.GetValue(Camera.GetInt32Setting(PropertyID.Av)).StringValue);
						TvComboBox.SelectedIndex =
							TvComboBox.Items.IndexOf(TvValues.GetValue(Camera.GetInt32Setting(PropertyID.Tv)).StringValue);
						ISOComboBox.SelectedIndex =
							ISOComboBox.Items.IndexOf(ISOValues.GetValue(Camera.GetInt32Setting(PropertyID.ISO)).StringValue);

						GUI.Camera_StatusUpdate("Connected with:" + Camera.DeviceName);
						GUI.Camera_OpenedSession(Camera);
						GUI.LiveViewButton_Click(null,null);
						IsConnected = true;
						IsGUIInit = true;
					});
				}
				catch (Exception e) {
					Console.WriteLine(e);
					ReportError(e.Message, false);
				}
			}
			else
				GUI.Dispatcher.Invoke((Action)delegate { GUI.Camera_StatusUpdate("No camera connected"); });
		}

		#region API Events

		private void APIHandler_CameraAdded(CanonAPI sender)
		{
			CamList = APIHandler.GetCameraList();
			OpenSession();
		}

		private void Camera_StateChanged(Camera sender, StateEventID eventID, int parameter)
		{
			try { if (eventID == StateEventID.Shutdown && IsInit) { CloseSession(); } }
			catch (Exception ex) { ReportError(ex.Message, false); }

		}

		#endregion

		#region Subroutines


		private void ReportError(string message, bool lockdown)
		{
			GUI.Dispatcher.Invoke((Action) delegate { GUI.Camera_ReportError(message, lockdown); });
		}

		#endregion

		public void TakePhoto(object sender, EventArgs args)
		{
			Camera.TakePhotoShutterAsync();
		}

		private void Camera_DownloadReady(Camera sender, DownloadInfo Info)
		{
			try {
				if (saveDirectory!=null)
				{
					sender.DownloadFile(Info, saveDirectory);
				}
				else
				{
					sender.DownloadFile(Info,  defSaveDir);
				}
			}
			catch (Exception ex) { ReportError(ex.Message, false); }
		}

		public void AvChanged(object sender, int value)
		{
			if (AvComboBox.SelectedIndex< 0 || !SessionOpen()) return;
			Camera.SetSetting(PropertyID.Av, AvValues.GetValue((string)AvComboBox.SelectedItem).IntValue);
			GUI.Camera_StatusUpdate("Av set to: " + value);
		}

		public void TvChanged(object sender, int value)
		{
			if (TvComboBox.SelectedIndex < 0 || !SessionOpen()) return;
			Camera.SetSetting(PropertyID.Tv, TvValues.GetValue((string)TvComboBox.SelectedItem).IntValue);
			GUI.Camera_StatusUpdate("Tv set to: " + value);
		}


		public void ISOChanged(object sender, int value)
		{
			if (ISOComboBox.SelectedIndex < 0 || !SessionOpen()) return;
			Camera.SetSetting(PropertyID.ISO, AvValues.GetValue((string)ISOComboBox.SelectedItem).IntValue);
			GUI.Camera_StatusUpdate("ISO set to: " + value);
		}

		public void LiveViewOnOff(object sender, EventArgs args)
		{
			try {
				if (!Camera.IsLiveViewOn)
				{
					Camera.StartLiveView();
				}
				else { Camera.StopLiveView();  }
			}
			catch (Exception ex) { ReportError(ex.Message, false); }
		}

		public void ChangeSaveDirectory(string dir)
		{
			saveDirectory = dir;
		}

		public bool IsLiveViewOn()
		{
			return Camera.IsLiveViewOn;
		}



		#region To Be Implemented

		public void FocusChanged(object sender, EventArgs args)
		{
			throw new NotImplementedException();
		}

		public void WBChanged(object sender, int args)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
/*					foreach (var Av in AvList)
						GUI.Invoke((Action)delegate { AvComboBox.Items.Add(AvComboBox.Items.Add(Av.StringValue)); });
					foreach (var Tv in TvList)
						GUI.Invoke((Action)delegate { TvComboBox.Items.Add(TvComboBox.Items.Add(Tv.StringValue)); });
					foreach (var ISO in ISOList)
						GUI.Invoke((Action)delegate { ISOComboBox.Items.Add(ISOComboBox.Items.Add(ISO.StringValue)); });
					GUI.Invoke((Action)delegate

					{
						AvComboBox.SelectedIndex =
							AvComboBox.Items.IndexOf(AvValues.GetValue(Camera.GetInt32Setting(PropertyID.Av)).StringValue);
					});
					GUI.Invoke((Action)delegate
					{
						TvComboBox.SelectedIndex =
							TvComboBox.Items.IndexOf(TvValues.GetValue(Camera.GetInt32Setting(PropertyID.Tv)).StringValue);
					});
					GUI.Invoke((Action)delegate
					{
						ISOComboBox.SelectedIndex =
							ISOComboBox.Items.IndexOf(ISOValues.GetValue(Camera.GetInt32Setting(PropertyID.ISO)).StringValue);
					});
					GUI.Invoke((Action)delegate
					{
						GUI.Camera_StatusUpdate("Connected with:" + Camera.DeviceName);
						IsConnected = true;
						GUI.Camera_OpenedSession(Camera);
						Camera.StartLiveView();
					});*/
