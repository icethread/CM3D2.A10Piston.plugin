using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace CM3D2.A10Piston.plugin
{
	/// <summary>
	/// レジストリからVorzeのUSBデバイスを参照するクラス.
	/// </summary>
	public class VorzeUSBSearcher
	{

		public static Dictionary<string, string> GetVorzeUSBDeviceList()
		{
			var vorzeUSBDeviceList = new Dictionary<string, string>();

			// CurrentControlSet内のVorzeUSBデバイスが登録されたキーを開く.
			RegistryKey parentKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\VID_10C4&PID_897C");

			// VorzeUSBのデバイス情報が登録したキーを開く.
			foreach (var deviceKeyName in parentKey.GetSubKeyNames())
			{
				// サブキーにてデバイス情報を取得.
				RegistryKey deviceKey = parentKey.OpenSubKey(deviceKeyName);

				// FriendlyNameを取得
				var deviceName = (string)deviceKey.GetValue("FriendlyName");

				// デバイスパラメータを取得.
				RegistryKey deviceParamKey = deviceKey.OpenSubKey("Device Parameters");

				var portName = (string)deviceParamKey.GetValue("PortName");

				// キーを閉じる.
				deviceParamKey.Close();
				deviceKey.Close();

				// 取得成功したらデバイス名をリストに追加.
				if (deviceName != null && portName != null)
				{
					vorzeUSBDeviceList.Add(portName, deviceName);
				}
			}

			// 親キーを閉じる
			parentKey.Close();

			// デバイス数が0より大きければ取得したデバイスのリストを返す
			return vorzeUSBDeviceList.Count > 0 ? vorzeUSBDeviceList : null;
		}
	}
}
