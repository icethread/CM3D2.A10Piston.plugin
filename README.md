## CM3D2.A10Cyclone.Plugin

カスタムメイド3D2に電動オナホ「[A10CycloneSA][]」および「[UFOSA][]」を連動させるプラグインです。

現状夜伽のみ対応  

F10キーを押すことにより実行された振動のパターンと強さをデバッグ情報として画面左上に表示されます。  
F11キーを押すことによりデバッグ用の再生ウィンドウが表示されます。  

ウィンドウ上にあるXML再読み込みボタンを押すことにより、ゲームを再起動する事なく  
各種振動設定XMLファイルを再読み込みして更新をする事ができます。  

PCに接続されている「[A10CycloneSA][]」と「[UFOSA][]」を自動判別し、
同時に振動設定XMLファイルにより同時に制御可能です。

## 不都合など
コマンド取得フックなどの関係で現状幾つかのプラグインと同居できない問題があります。

現状同居できないプラグイン  
・AddYotogiSlider

・CM3D2.CycloneX10.Plugin

## 開発・動作環境
カスタムメイド3D2	Ver1.60

## 導入方法

### 前提条件  : **Sybarys** 又は **UnityInjector** が導入済みであること。  

[![ダウンロードボタン][img_download]][master zip]を押してzipファイルをダウンロード。   
解凍後CM3D2.A10Cyclone.Plugin.dllファイルを./Sybaris/Plugins/UnityInjector/フォルダに入れる。

特定のフォーマットで記載したxmlファイルを指定のディレクトリに入れる事により動作します。

./Sybaris/Plugins/UnityInjector/Config/A10CycloneXml/

※フォルダが無い場合は自動で生成されます。

## 注意書き

個人で楽しむ為の非公式Modです。  
転載・再配布・改変・改変物配布等は「KISS」又は「Vorze」に迷惑のかからぬ様、  
各自の判断・責任の下で行って下さい。  
ハードが絡むプラグインの為、よくテストをしてからご使用ください。  ***(もげても責任取れません)***

## 設定ファイルについて
全夜伽グループ名+コマンドを表記したxmlファイルをZipで同封しました。   
各夜伽コマンドの設定はそのZip内のファイルを元に加筆、修正をしてください。  

***○ EditInformation (任意)***  
編集者、編集日、コメントなど 項目自体省略可能

```
<EditInformation>
    <EditName>編集者名</EditName>         
    <TimeStamp>タイムスタンプ</TimeStamp>
    <Comment>自由に記載OK</Comment>
  </EditInformation>
```

***○ 識別名 (必須)***  
振動させるタイプを定義する項目です。  
必要な分だけ作成をしてください。

・Lv0		振動の強さ(0-127の数字) 興奮値 0以下  
・Lv1		振動の強さ(0-127の数字) 興奮値 0-100   
・Lv2		振動の強さ(0-127の数字) 興奮値 100-200   
・Lv3		振動の強さ(0-127の数字) 興奮値 200以上   
・識別名	識別する名前  ↓の例でいう「STOP」や「PreSet0」、「PreSet1」

```
  <LevelList>
    <LevelItem Lv0="0" Lv1="0" Lv2="0" Lv3="0">STOP</LevelItem>
    <LevelItem Lv0="1" Lv1="1" Lv2="3" Lv3="5">PreSet0</LevelItem>
    <LevelItem Lv0="1" Lv1="2" Lv2="3" Lv3="4">PreSet1</LevelItem>
    <LevelItem Lv0="1" Lv1="3" Lv2="5" Lv3="7">PreSet2</LevelItem>
    <LevelItem Lv0="1" Lv1="4" Lv2="7" Lv3="9">PreSet3</LevelItem>
  </LevelList>
```

***○ YotogiCXConfig (必須)***  
振動させるデータを定義する項目
GroupNameはゲーム内の夜伽グループ名と同じ物を入力する事
```
<YotogiCXConfig>
    <GroupName>処女喪失セックス</GroupName>
    <YotogiList>
	    <!--ここは次項を参照-->
        <YotogiItem>～～～</YotogiItem>
		<YotogiItem>～～～</YotogiItem>
    </YotogiList>
<YotogiCXConfig>
```

***○ YotogiItem (必須)***  
振動させる夜伽グループの中のコマンドを定義する項目
Yotogi_Nameはゲーム内の夜伽コマンドと同じ物を入力する事

< Insert >は将来の為に項目だけ作成、省略可能  
挿入時のアニメーションを定義する予定

```
      <YotogiItem>
        <Yotogi_Name>責める</Yotogi_Name>
        <ControlData>
          <Control />
          <Control />
          <Control />
        </ControlData>
      </YotogiItem>
```
***○ Control (必須)***  
振動のパターンを並べて細かい動きを設定できます。
記載されているデータは上から順番に処理し、末尾まで処理完了したら
先頭から再度繰り返します。

Controlで使用できる属性一覧  
・Pattern	：指定した振動パターンにする(0=正転、1=逆転)  
・Level		：指定した振動レベルにする(0=停止)  
・LvName	：興奮値による振動レベルを適用(LevelListの識別名と一致させる事)  
・Delay		：実行前に指定したDelay(秒)待機をする  
・Time		：実行後に指定したTime(秒)待機をする  (指定の無い場合はデフォルトで0.1秒の継続を行います)  
・Insert	：trueの場合非挿入→挿入時のみ実行をする  
・Personal	：メイドの性格が一致する場合に実行をする  
・Device    ：制御対象のデバイスを「[A10CycloneSA][]」、「[UFOSA][]」のどちらにするか選択します。指定がない場合は両方に対応します。("A10Cyclone"= A10サイクロンSAを制御、"UFOSA"= U.F.O SAを制御)  
・Excite	：興奮レベルが一致する場合に実行(興奮値とレベルの対応はLvName属性と同様)

 LevelとLevelNameがそれぞれ定義されていた場合はLevelNameを優先します。

○停止させるには  
 < Control Level="0" /> =>停止と同等  

○Insertフラグ(挿入時のみ実行する)
```
<Normal>
  <!--挿入時のみ実行-->
  <Control Pattern="0" Level="0" Time="0.8" Insert="true" />   停止 0.8秒後に次項目
  <Control Pattern="0" Level="1" Time="0.2" Insert="true" />   パターン 0 振動レベル 1 で0.2秒後に次項目
  <Control Pattern="0" Level="0" Time="0.5" Insert="true" />   停止 0.5秒後に次項目
  <Control Pattern="0" Level="1" Time="1.0" Insert="true" />   パターン 0 振動レベル 1 で1.0秒後に次項目
  <Control Pattern="0" Level="0" Time="0.5" Insert="true" />   停止 0.5秒後に次項目
  <Control Pattern="3" Level="1" Time="1.0" Insert="true" />   パターン 3 振動レベル 1 で1.0秒後に次項目
  <!--通常責め時-->
  <Control Pattern="3" LvName="PreSet3" />       パターン 3 興奮値のレベルで振動を続ける
</Normal>
```
○Personal設定(メイドの性格に応じたパターン設定)
```
<Normal>
  <!--おねだり中(純真妹系)-->
  <Control Pattern="4" Level="3" Time="15.0" Personal="Pure" />
  <Control Pattern="8" Level="6" Time="30.0" Personal="Pure" />
  <!--おねだり中(クール)-->
  <Control Pattern="4" Level="3" Time="15.0" Personal="Cool" />
  <Control Pattern="8" Level="6" Time="30.0" Personal="Cool" />
  <!--おねだり中(プライド)-->
  <Control Pattern="4" Level="3" Time="15.0" Personal="Pride" />
  <Control Pattern="8" Level="6" Time="30.0" Personal="Pride" />
  <!--中出しサンプル-->
  <Control Pattern="0" Level="0" Time="0.8" />
  <Control Pattern="0" Level="3" Time="0.2" />
</Normal>
```
○Device設定(A10サイクロンSA、U.F.O SA同期制御用)
```
<Normal>
  <!--A10サイクロンSAを制御-->
  <Control Pattern="4" Level="3" Time="10.0" Device="A10Cyclone" />
  <!--U.F.O SAを制御-->
  <Control Pattern="4" Level="3" Time="10.0" Device="UFOSA" />
  <!--指定なしで両方のデバイスを制御-->
  <Control Pattern="4" Level="3" Time="10.0" />
</Normal>
```

## 本プラグインについて
本プラグインは、[CM3D2.A10Cyclone.Plugin][]をフォークさせて頂き、機能追加を行っております。

[A10CycloneSA]: http://www.vorze.jp/a10cyclonesa/ "A10サイクロンSA"
[UFOSA]: http://www.vorze.jp/ufosa/ "U.F.O.SA"
[CM3D2.A10Cyclone.Plugin]: https://github.com/obtdai/CM3D2.A10Cyclone.plugin/ "CM3D2.AddModsSlider.Plugin/"
[master zip]: https://github.com/icethread/CM3D2.A10Cyclone.plugin/archive/master.zip "master zip"
[img_download]: http://i.imgur.com/byav3Uf.png "ダウンロードボタン"
