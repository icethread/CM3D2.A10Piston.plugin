using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using UnityEngine;
using UnityInjector.Attributes;

using A10Piston;
using System.Xml.Serialization;

namespace CM3D2.A10Piston.plugin
{
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginFilter("CM3D2VRx64")]
	[PluginName(PluginName), PluginVersion(Version)]
	public class A10Piston : UnityInjector.PluginBase
	{
		private const string PluginName = "CM3D2 A10Piston Plugin";
		private const string Version = "0.0.0.1";


		//XMLファイルの読み込み先
		private readonly string XmlFileDirectory = Application.dataPath + "/../UnityInjector/Config/A10PistonXml/";

		//各種設定項目
		private readonly float TimePerInit = 1.00f;
		private readonly float WaitFirstInit = 5.00f;

		//初期化完了かどうか
		private bool InitCompleted = false;

		//動作中のステータス
		private string yotogi_group_name = "";          //夜伽グループ名
		private string yotogi_name = "";                //夜伽名
		private int iLastExcite = 0;                    //興奮値
		private Yotogi.ExcitementStatus yExciteStatus;  //興奮ステータス
		private YotogiPlay.PlayerState bInsertFuck = YotogiPlay.PlayerState.Normal;               //挿入状態かどうか
		private string Personal="";

		// CM3D2関連の参照
		private int sceneLevel;//シーンレベル
		private Maid maid;
		private YotogiManager yotogiManager;
		private YotogiPlayManager yotogiPlayManager;
		private Action<Yotogi.SkillData.Command.Data> orgOnClickCommand;

		//A10Piston関連
		private List<A10PistonClass> devices = new List<A10PistonClass>();
		private static bool PistonGUI = false;
		private Rect windowRect = new Rect(20, 20, 120, 50);
		private int NowPattern = 0;
		private int NowLevel = 0;

		//サイクロン用の設定ファイル郡
		private A10PistonConfig.YotogiItem YotogiItem = null;
		private SortedDictionary<string, A10PistonConfig> A10PistonConfigDictionay = new SortedDictionary<string, A10PistonConfig>();
		private Dictionary<string, A10PistonConfig.LevelItem> A10PistonLevelsDict = new Dictionary<string, A10PistonConfig.LevelItem>();

		//コルーチン
		private List<IEnumerator> PistonEnums = new List<IEnumerator>();

		#region MonoBehaviour methods
		public void Start()
		{
			//設定用のディレクトリを生成する
			if (!System.IO.Directory.Exists(XmlFileDirectory))
			{
				System.IO.Directory.CreateDirectory(XmlFileDirectory);
				Debug.Log("ディレクトリ生成:" + XmlFileDirectory);
			}

			//デッバク用ログ初期設定
			DebugManager.DebugMode = false;
		}

		public void Update()
		{
			if (sceneLevel == 14)
			{
				//ログを表示する
				if (Input.GetKeyDown(KeyCode.F10))
				{
					DebugManager.DebugMode = !DebugManager.DebugMode;
				}
				//A10サイクロン関連のダイアログ表示
				if (Input.GetKeyDown(KeyCode.F11))
				{
					PistonGUI = !PistonGUI;
				}
			}
		}

		public void OnGUI()
		{
			//デバッグ機能
			if (InitCompleted && sceneLevel == 14)
			{
				//ログ表示
				DebugManager.GUIText();
				//A10サイクロン関連のデバッグ用ウィンドウ
				if (PistonGUI)
					windowRect = GUILayout.Window(0, windowRect, GUIWindow, "A10Piston");
			}
		}

		public void OnApplicationQuit()
		{
			A10PistonInit();
		}

		public void A10PistonInit()
		{
			//Stopする
			devices.ForEach(device => device.SetPatternAndLevel(0, 0));

			//変数群初期化
			yotogi_group_name = "";
			yotogi_name = "";
			iLastExcite = 0;
			yExciteStatus = 0;
			bInsertFuck = YotogiPlay.PlayerState.Normal;
			Personal = "";

			NowPattern = 0;
			NowLevel = 0;
		}

		//シーンがロードされた場合
		public void OnLevelWasLoaded(int level)
		{
			//夜伽シーンの場合初期化をする
			if (level == 14)
			{
				//起動時に読み込み
				LoadPistonXMLFile();
				//初期化
				StartCoroutine(initCoroutine(TimePerInit));
			}
			A10PistonInit();

			//読み込んだシーンレベルを保存
			sceneLevel = level;
		}
		#endregion

		#region MonoBehaviour Coroutine

		private IEnumerator initCoroutine(float waitTime)
		{
			yield return new WaitForSeconds(WaitFirstInit);
			while (!(InitCompleted = Yotogi_initialize())) yield return new WaitForSeconds(waitTime);
			DebugManager.Log("Initialization complete [ Load SeenLevel:" + sceneLevel.ToString() + "]");
		}

		private IEnumerator PistonCoroutine(int iLastExcite, A10PistonConfig.YotogiItem YotogiItem, Dictionary<string, A10PistonConfig.LevelItem> A10PistonPattanDict, bool InsertFlg, string Personal, A10PistonClass a10Piston)
		{
			// 興奮状態のステータス
			yExciteStatus = YotogiPlay.GetExcitementStatus(iLastExcite);
			int iExciteStatus = (int)yExciteStatus;

			// 指定コマンドの制御状態をループする.
			while (true)
			{
				foreach (A10PistonConfig.Control Item in YotogiItem.ControlData)
				{
					// 性格を指定しているが不一致の場合は無視
					if (Item.Personal != "" && Item.Personal != Personal)
					{
						continue;
					}

					// 挿入時指定をしているが挿入フラグがない場合は無視
					if (Item.Insert && !InsertFlg)
					{
						continue;
					}

					// 接続機器を指定しているが不一致の場合は無視
					if (Item.Device != "" && Item.Device != a10Piston.ConnectedModel.ToString())
					{
						continue;
					}

					// 興奮状態を指定しているが不一致の場合は無視
					if (0 <= Item.Excite && Item.Excite != iExciteStatus)
					{
						continue;
					}

					//現在のPatternとLevel
					A10PistonClass.Pattern SetPattan = a10Piston.pattern;
					int SetLevel = a10Piston.level;
					int SetPosition = a10Piston.position;

					//Patternの定義があれば更新
					if (0 == Item.Pattern)
					{
						SetPattan = A10PistonClass.Pattern.ClockWise;

					}
					else if (1 == Item.Pattern)
					{
						SetPattan = A10PistonClass.Pattern.CounterClockWise;
					}

					//Positionの定義があれば更新
					if (-1 < Item.Position)
					{
						SetPosition = Clamp(Item.Position, A10PistonClass.Position_Min, A10PistonClass.Position_Max);
					}

					//Levelの定義があれば更新
					if (-1 < Item.Level)
					{
						SetLevel = Clamp(Item.Level, A10PistonClass.Level_Min, A10PistonClass.Level_Max);
					}

					//LevelNameの定義がある場合
					if (Item.LvName != "")
					{
						if (A10PistonPattanDict.ContainsKey(Item.LvName))
						{
							//興奮値を元にLevelを更新
							SetLevel = Clamp(GetLevel(yExciteStatus, A10PistonPattanDict[Item.LvName]), A10PistonClass.Level_Min, A10PistonClass.Level_Max);
						}
						else
						{
							DebugManager.Log("LevelNameの定義が見つかりません");
						}
					}

					//ディレイ
					if (0.0f < Item.Delay)
					{
						yield return new WaitForSeconds(Item.Delay);
					}

					//振動を開始する
					if (SetLevel != a10Piston.level || SetPattan != a10Piston.pattern || SetPosition != a10Piston.position)
					{
						//Pistonの振動処理
						//a10Piston.SetPatternAndLevel(SetPattan, SetLevel);
						a10Piston.SetPositionAndLevel(SetPosition, SetLevel);
						//GUI用に更新をする。
						NowPattern = (Int32)a10Piston.pattern;
						NowLevel = a10Piston.level;
					}

					//ログを追加
					DebugManager.Log(a10Piston.ConnectedModel.ToString() + ": [Pattern:" + a10Piston.pattern + "][Level:" + a10Piston.level + "][Delay:" + Item.Delay + "][Time:" + Item.Time + "]");

					//継続タイム
					if (0.0f < Item.Time)
					{
						yield return new WaitForSeconds(Item.Time);
					}
					else
					{
						// 継続時間の指定が無い場合、0.1秒毎に次の処理へ移行する
						yield return new WaitForSeconds(0.1f);
					}

					/*
					//性格の指定があるかどうか(未指定の場合はそのまま実行)
					if (Item.Personal == "" || Item.Personal == Personal)
					{
						//挿入時に挿入フラグがあった場合もしくはそれ以外
						if ((Item.Insert && InsertFlg) || Item.Insert == false)
						{
							// 接続機器の指定があるかどうか(未指定の場合はそのまま実行)
							if ((Item.Device == "" || Item.Device == a10Piston.ConnectedModel.ToString()))
							{
								// 興奮状態が指定と異なる場合は実行しない(未指定の場合はそのまま実行)
								if (0 <= Item.Excite && Item.Excite != iExciteStatus)
								{
									continue;
								}
							}
						}
					}
					*/
				}

				// 2ループ目以降の挿入フラグはオフにする
				InsertFlg = false;
			}
		}
		#endregion

		#region MonoBehaviour GUI関連

		/// <summary>
		/// Piston用の操作Window
		/// </summary>
		/// <param name="windowID"></param>
		private void GUIWindow(int windowID)
		{
			GUILayout.BeginHorizontal();
			{
				foreach( var device in devices)
				{
					GUILayout.Label(device.ConnectedModel.ToString());
					if (device.IsDeviceEnable)
					{
						GUILayout.Label("接続状態: 接続中");
					}
					else
					{
						GUILayout.Label("接続状態: 未接続");
					}
				}
				if (GUILayout.Button("XML再読み込み"))
				{
					LoadPistonXMLFile();
				}

				//通常このXML出力機能は使わない
				//if (GUILayout.Button("出力"))
				//{
				//	CreateAllYotogiXML();
				//}

			}
			GUILayout.EndHorizontal();

			GUILayout.Label("Pattern");

			GUILayout.BeginHorizontal();
			{
				for (int i = 0; i < 2; i++)
				{
					if (GUILayout.Toggle(i == NowPattern, i.ToString()))
					{
						NowPattern = i;
					}
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Label("Level");

			GUILayout.BeginHorizontal();
			{
				for (int i = 0; i < 6; i++)
				{
					if (GUILayout.Toggle((i*10) == NowLevel, (i*10).ToString()))
					{
						NowLevel = (i*10);
					}
				}

				foreach (var device in devices)
				{
					if (NowLevel != device.level || NowPattern != (Int32)device.pattern)
					{
						device.SetPatternAndLevel(NowPattern == 0 ? A10PistonClass.Pattern.ClockWise : A10PistonClass.Pattern.CounterClockWise, NowLevel);
						NowPattern = (Int32)device.pattern;
						NowLevel = device.level;
					}
					DebugManager.Log("SetPatternAndLevel:" + NowPattern + "," + NowLevel);
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Label("デバイス接続／切断");
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("再接続"))
			{
				CloseVorzeDevices();
				OpenVorzeDevices();
			}
			if (GUILayout.Button("切断"))
			{
				CloseVorzeDevices();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			foreach (var device in devices)
			{
				if (GUILayout.Button(device.ConnectedModel.ToString() + " ポーズ:" + device.IsPause))
				{
					device.Pause();
				}
				DebugManager.Log(device.ConnectedModel.ToString() + " ポーズ:" + device.IsPause.ToString());

			}
			GUILayout.EndHorizontal();

			GUILayout.Label("夜伽グループ:" + yotogi_group_name);
			GUILayout.Label("夜伽コマンド:" + yotogi_name);
			GUILayout.Label("興奮値　　　:" + iLastExcite.ToString() + "[" + yExciteStatus+"]");
			GUILayout.Label("挿入状態　　:" + bInsertFuck.ToString());
			GUILayout.Label("メイド性格　:" + Personal);
			GUI.DragWindow();
		}

		/// <summary>
		/// 画面上に常に表示をするデバッグ機能
		/// </summary>
		private static class DebugManager
		{
			public static bool DebugMode
			{
				get { return _DebugMode; }
				set { _DebugMode = value; }
			}
			//デバッグの最大行数
			private const int MaxDebugText = 10;

			private static bool _DebugMode = false;
			private static Queue<string> DebugTextList = new Queue<string>();
			private static Rect TextAreaRect = new Rect(10, 10, Screen.width / 2, Screen.height - 20);

			//デバッグ情報として出力する内容
			public static void Log(string DebugText)
			{
				if (MaxDebugText < DebugTextList.Count)
				{
					//先頭の物を削除
					DebugTextList.Dequeue();
					DebugTextList.Enqueue(DebugText);
				}
				else
				{
					DebugTextList.Enqueue(DebugText);
				}
			}
			//クリア
			public static void Clear()
			{
				DebugTextList.Clear();
			}

			//OnGUI上で実行すること
			public static void GUIText()
			{
				if (DebugMode)
				{
					GUILayout.BeginArea(TextAreaRect);
					foreach (string log in DebugTextList)
					{
						GUILayout.Label(log);
					}
					GUILayout.EndArea();
				}
			}

		}
		#endregion


		/// <summary>
		/// デバイスクローズ
		/// </summary>
		private void CloseVorzeDevices()
		{
			foreach (var device in devices)
			{
				device.CloseDevice();
			}
			devices.Clear();
		}

		/// <summary>
		/// デバイスを開く.
		/// </summary>
		private void OpenVorzeDevices()
		{
			// システム登録済のデバイス一覧を取得.
			var deviceList = VorzeUSBSearcher.GetVorzeUSBDeviceList();

			// デバイス一覧が空でなければ、デバイスを開く
			if (deviceList != null)
			{
				foreach (var keyValue in deviceList)
				{
					var device = new A10PistonClass();

					// デバイスのオープンに成功したら、リスト登録
					if (device.OpenDevice(keyValue.Key))
					{
						devices.Add(device);
					}
				}
			}
		}

		#region UnityInjector関連
		private bool Yotogi_initialize()
		{
			// デバイスオープン
			OpenVorzeDevices();

			//メイドを取得
			this.maid = GameMain.Instance.CharacterMgr.GetMaid(0);
			if (!this.maid) return false;

			// 夜伽コマンドフック
			{
				this.yotogiManager = getInstance<YotogiManager>();
				if (!this.yotogiManager) return false;
				this.yotogiPlayManager = getInstance<YotogiPlayManager>();
				if (!this.yotogiPlayManager) return false;

				YotogiCommandFactory cf = getFieldValue<YotogiPlayManager, YotogiCommandFactory>(this.yotogiPlayManager, "command_factory_");
				if (IsNull(cf)) return false;

				try
				{
					//YotogiPlayManagerのコールバック
					cf.SetCommandCallback(new YotogiCommandFactory.CommandCallback(this.OnYotogiPlayManagerOnClickCommand));
				}
				catch (Exception ex)
				{
					DebugManager.Log(string.Format("Error - SetCommandCallback() : {0}", ex.Message));
					return false;
				}

				this.orgOnClickCommand = getMethodDelegate<YotogiPlayManager, Action<Yotogi.SkillData.Command.Data>>(this.yotogiPlayManager, "OnClickCommand");
				if (IsNull(this.orgOnClickCommand)) return false;
			}
			return true;
		}

		public void OnYotogiPlayManagerOnClickCommand(Yotogi.SkillData.Command.Data command_data)
		{
			YotogiPlay.PlayerState OldPlayerState = bInsertFuck;

			//実際の動作をする
			orgOnClickCommand(command_data);

			//メイドの性格を取得
			Personal = this.maid.Param.status.personal.ToString();

			//夜伽グループ名
			yotogi_group_name = command_data.basic.group_name;
			//夜伽コマンド名
			yotogi_name = command_data.basic.name;
			//興奮値
			iLastExcite = maid.Param.status.cur_excite;
			//興奮状態のステータス
			yExciteStatus = YotogiPlay.GetExcitementStatus(iLastExcite);
			//挿入状態かどうか
			bInsertFuck = getFieldValue<YotogiPlayManager, YotogiPlay.PlayerState>(this.yotogiPlayManager, "player_state_");

			//PlayerStateがNormalからInsertになる場合
			bool InsertFlg = (OldPlayerState == YotogiPlay.PlayerState.Normal && bInsertFuck == YotogiPlay.PlayerState.Insert);

			//Pistonを実行する
			A10PistonEvents(yotogi_group_name, yotogi_name, iLastExcite, InsertFlg, Personal);
		}
		#endregion

		#region A10Piston関連
		private void A10PistonEvents(string yotogi_group_name, string yotogi_name, int iLastExcite, bool InsertFlg ,string Personal)
		{
			//前回のコルーチンが走っている場合は停止をする
			foreach (var PistonEnum in PistonEnums)
			{
				StopCoroutine(PistonEnum);
			}
			PistonEnums.Clear();

			YotogiItem = null;
			A10PistonLevelsDict.Clear();
			if (A10PistonConfigDictionay.ContainsKey(yotogi_group_name))
			{
				//振動パターンのDictionayを生成
				foreach (A10PistonConfig.LevelItem Item in A10PistonConfigDictionay[yotogi_group_name].LevelList)
				{
					if (!A10PistonLevelsDict.ContainsKey(Item.LvName))
					{
						A10PistonLevelsDict.Add(Item.LvName, Item);
					}
					else
					{
						Debug.Log("Warning : LevelNameが重複しています。[" + Item.LvName + "]");
					}
				}
				//設定ファイルを確定する
				foreach (A10PistonConfig.YotogiItem Item in A10PistonConfigDictionay[yotogi_group_name].YotogiCXConfig.YotogiList)
				{
					if (Item.Yotogi_Name == yotogi_name)
					{
						YotogiItem = Item;
						break;
					}
				}
				if (YotogiItem != null)
				{
					DebugManager.Log("実行:" + YotogiItem.Yotogi_Name);

					//コルーチンを開始する
					foreach (var device in devices)
					{
						PistonEnums.Add(PistonCoroutine(iLastExcite, YotogiItem, A10PistonLevelsDict, InsertFlg, Personal, device));
					}
					foreach (var PistonEnum in PistonEnums)
					{
						StartCoroutine(PistonEnum);
					}
				}
			}
		}

		//導入されている全夜伽コマンドデータ用の設定ファイルを一括作成
		void CreateAllYotogiXML()
		{
			for (int cat = 0; cat < (int)Yotogi.Category.MAX; cat++)
			{
				SortedDictionary<int, Yotogi.SkillData> data = Yotogi.skill_data_list[cat];
				foreach (Yotogi.SkillData sd in data.Values)
				{
					A10PistonConfig XML = new A10PistonConfig();
					XML.EditInformation.EditName = "UserName";
					XML.EditInformation.TimeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
					XML.EditInformation.Comment = "";

					XML.YotogiCXConfig.GroupName = sd.name;
					XML.LevelList.Clear();
					XML.LevelList.Add(PatternItem("STOP", 0, 0, 0, 0));
					XML.LevelList.Add(PatternItem("PreSet1", 10, 30, 50, 70));
					XML.LevelList.Add(PatternItem("PreSet2", 20, 40, 60, 80));
					XML.LevelList.Add(PatternItem("PreSet3", 60, 80, 100, 120));

					foreach (var comData in sd.command.data)
					{
						A10PistonConfig.YotogiItem YotogiListData = new A10PistonConfig.YotogiItem();

						YotogiListData.Yotogi_Name = comData.basic.name;
						YotogiListData.ControlData.Add(ControlItem(0f, "STOP"));

						XML.YotogiCXConfig.YotogiList.Add(YotogiListData);
					}

					XMLWriter<A10PistonConfig>(XmlFileDirectory + sd.name + ".xml", XML);
				}
			}
		}

		private A10PistonConfig.LevelItem PatternItem(string Name, int LV0, int LV1, int LV2, int LV3)
		{
			A10PistonConfig.LevelItem PItem = new A10PistonConfig.LevelItem();
			PItem.LvName = Name;
			PItem.Lv0 = LV0;
			PItem.Lv1 = LV1;
			PItem.Lv2 = LV2;
			PItem.Lv3 = LV3;
			return PItem;
		}
		private A10PistonConfig.Control ControlItem(float diray , string Name)
		{
			A10PistonConfig.Control Cont = new A10PistonConfig.Control();
			Cont.Delay = diray;
			Cont.LvName = Name;
			return Cont;
		}

		/// <summary>
		/// 振動設定用のXMLファイル
		/// </summary>
		private void LoadPistonXMLFile()
		{
			Debug.Log("読み込み開始");
			A10PistonConfigDictionay.Clear();
			string[] files = System.IO.Directory.GetFiles(XmlFileDirectory, "*.xml", System.IO.SearchOption.AllDirectories);
			foreach (string file in files)
			{
				try
				{
					if (System.IO.File.Exists(file))
					{
						A10PistonConfig XML = XMLLoader<A10PistonConfig>(file);
						if (!A10PistonConfigDictionay.ContainsKey(XML.YotogiCXConfig.GroupName))
						{
							A10PistonConfigDictionay.Add(XML.YotogiCXConfig.GroupName, XML);
						}
					}
				}
				catch (Exception err)
				{
					//エラーが有った場合のみエラー内容を表示
					Debug.Log(System.IO.Path.GetFileName(file) + ":LoadError [" + err + "] ");
				}
			}
			Debug.Log("A10Pistonの設定ファイル " + A10PistonConfigDictionay.Count + "個 読み込み完了");
		}

		#endregion

		#region 各種関数群
		/// <summary>
		/// XMLデータの読み込み
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static T XMLLoader<T>(string path)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(T));

			FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			StreamReader reader = new System.IO.StreamReader(stream, new System.Text.UTF8Encoding(false));
			T load = (T)serializer.Deserialize(reader);
			reader.Close();

			return load;
		}
		/// <summary>
		/// XMLデータの書き込み
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <param name="save"></param>
		public static void XMLWriter<T>(string path, T save)
		{
			//XMLファイルに保存する
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			System.IO.StreamWriter writer = new System.IO.StreamWriter(path, false, new System.Text.UTF8Encoding(false));
			serializer.Serialize(writer, save);
			writer.Close();
		}


		//ゲームオブジェクトの検索と取得
		internal static T getInstance<T>() where T : UnityEngine.Object
		{
			return UnityEngine.Object.FindObjectOfType(typeof(T)) as T;
		}
		//IsNUll
		internal static bool IsNull<T>(T t) where T : class
		{
			return (t == null) ? true : false;
		}

		internal static TResult getFieldValue<T, TResult>(T inst, string name)
		{
			if (inst == null) return default(TResult);

			FieldInfo field = getFieldInfo<T>(name);
			if (field == null) return default(TResult);

			return (TResult)field.GetValue(inst);
		}
		internal static FieldInfo getFieldInfo<T>(string name)
		{
			BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

			return typeof(T).GetField(name, bf);
		}

		internal static TResult getMethodDelegate<T, TResult>(T inst, string name)
			where T : class
			where TResult : class
		{
			return Delegate.CreateDelegate(typeof(TResult), inst, name) as TResult;
		}

		private int GetLevel(Yotogi.ExcitementStatus Status, A10PistonConfig.LevelItem LevelItem)
		{
			try
			{
				switch (Status)
				{
					case Yotogi.ExcitementStatus.Minus:
						{
							return LevelItem.Lv0;
						}
					case Yotogi.ExcitementStatus.Small:
						{
							return LevelItem.Lv1;
						}
					case Yotogi.ExcitementStatus.Medium:
						{
							return LevelItem.Lv2;
						}
					case Yotogi.ExcitementStatus.Large:
						{
							return LevelItem.Lv3;
						}
					default:
						{
							return -1;
						}
				}
			}
			catch
			{
				Debug.Log("Error:GetLevel");
				return -1;
			}
		}

		/// 値の最大最小を制限する
		private static int Clamp(int value, int Min, int Max)
		{
			if (value < Min)
			{
				return Min;
			}
			else if (Max < value)
			{
				return Max;
			}
			else
			{
				return value;
			}
		}

		#endregion

	}
}