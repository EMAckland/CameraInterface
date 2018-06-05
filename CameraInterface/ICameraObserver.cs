using System;
using System.Windows;

namespace CameraInterface
{
	public interface ICameraObserver
	{

		void TakePhoto(Object sender, EventArgs args);

		void FocusChanged(Object sender, EventArgs args);

		void AvChanged(Object sender, int args);

		void TvChanged(Object sender, int args);

		void WBChanged(Object sender, int args);

		void ISOChanged(Object sender, int args);

		void LiveViewOnOff(Object sender, EventArgs args);

		void ChangeSaveDirectory(String dir);

		bool IsLiveViewOn();

		bool SessionOpen();

		void CloseSession();

		void SetInitGUI();
		void Init();
		Window GetCameraWindow();
		void SetGUI(Window CameraWindow);


	}
}
