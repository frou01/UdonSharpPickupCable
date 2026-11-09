# UdonSharpPickupCable

VRChat ワールド向けの、手で抜き差しできる多芯ケーブルのライブラリです（UdonSharp）。

ケーブルをつなぐと、つながった機器どうしで値を共有できます。途中で分岐するケーブル、Pin の足りないソケット、クロス配線も扱えます。芯ごとにコンポーネントを作る必要はありません。

> バージョン 0.0.1 の実験的なパッケージです。名前や仕様は変わることがあります。

## 必要なもの

- Unity 2022.3
- VRChat Worlds SDK 3.7.4 以降（`com.vrchat.worlds`）
- `com.frou01.lupick` 7.1.0 以降（プラグとソケットの抜き差し）
- `com.frou01.utils` 9.1.0 以降

## 試してみる

`Samples/Scenes/` に動作確認用のシーンがあります。

| シーン | 内容 |
|---|---|
| `WireTest_SingleFloat` | 芯が1本（`float`）のケーブル |
| `WireTest_MultiWire` | 芯が4本（`float` / `float` / `int` / `UdonSharpBehaviour`）のケーブル |

機器側の書き方の例は `Samples/Scripts/` にあります。

## 用語

| 用語 | 意味 |
|---|---|
| Cable | Wire と Terminal を持つもの。ケーブル、車体内の配線、機器の接続部など |
| Wire | Cable の中の1本の芯。番号は Cable の中で共通 |
| Terminal | Cable の端（プラグやソケット）。Pin を持ち、相手の Terminal と1対1でつながる。1つの Cable に複数置ける |
| Pin | Terminal の中の接点。相手の**同じ番号**の Pin とつながる |
| Net | 電気的につながっている Wire の集まり |
| 疑似ポインタ | Net で共有する値の入れ物（`float[]` などの配列） |
| Writer / Reader | 疑似ポインタに書く Wire と、読むだけの Wire |

## 基本の使い方

### 1. Cable と Terminal を置く

1. GameObject に `Cable` を付けます。ただの配線なら基底の `Cable` のままで使えます。機器につなぐ Cable は「機器をつなぐ」のように継承して作ります。
2. ケーブルの端ごとに `Terminal` を付けて、次の2つを設定します。
   - `cable`: この Terminal が属する Cable
   - `pinToWireMap`: Pin 番号ごとの Wire 番号（下記）

Cable の `terminals` は、シーンの処理時に自動で設定されるので、設定は要りません。

### 2. Pin と Wire を対応させる

`pinToWireMap` のインデックスが Pin 番号、値が Wire 番号です。その Pin がないときは `-1` にします。1つの Wire を複数の Pin に割り当ててもかまいません。

```
pinToWireMap = { 0, 1, -1, 2 }
// Pin 0 → Wire 0
// Pin 1 → Wire 1
// Pin 2 → なし
// Pin 3 → Wire 2
```

Pin は「相手と向かい合う位置の番号」で、物理的なピン番号とは違うことがあります。

- **Pin の数が相手と違うとき:** 相手にない番号の Pin は、つながっていないものとして扱います。
- **並びが反転するコネクタ:** 反転した割り当てにして吸収します。

### 3. 抜き差しできるようにする

LUPick の仕組みの上で動くコンポーネントを使います。LUPick 自体の設定は、LUPick の説明を見てください。

- プラグ: `PlugPickup` を付けて、`linkedTerminal` に Terminal を設定します。
- ソケット: `SocketCatchCollider` を付けて、`linkedTerminal` に Terminal を設定します。

ソケットにプラグが入ると Terminal がつながり、抜けると切れます。標準では、ソケットは空いていれば `PlugPickup` をどれでも受け入れます（受け入れ条件の変え方は「接続できるプラグを制限する」）。

スクリプトから直接つなぐこともできます。

```csharp
terminalA.Connect(terminalB); // すでにつながっていれば、先に切ってからつなぐ
terminalA._Disconnect();
```

**同期について:** このパッケージは接続状態も疑似ポインタの値も同期しません。ローカルで「つながった・切れた」を受けて、疑似ポインタを配り直すだけです。プレイヤー間で揃える処理は、別に用意してください。

## 機器をつなぐ

`Cable` を継承して、必要なメソッドをオーバーライドします。`float` を1つ受け渡す例です（`Samples/Scripts/SimpleFloatCable.cs` を短くしたもの）。

```csharp
public class MyCable : Cable
{
    public bool isWriter;
    public float defaultValue;
    public float valueToWrite;

    [NonSerialized] public float[] pointer;

    void Start()
    {
        // 同じ Net の別の Cable が先に配り直していれば、やらなくてよい
        if (currentPointers != null && currentPointers.Length > 0 && currentPointers[0] != null) return;
        RequestRefresh(0);
    }

    // Wire ごとの配列を作る。Wire の数はこの長さで決まる
    protected override object[] CreateSourcePointers()
    {
        object[] sourcePointers = new object[1];
        sourcePointers[0] = new float[1];
        return sourcePointers;
    }

    public override bool IsWriter(int wire)
    {
        return isWriter;
    }

    // 要素だけを書き換える。配列を作り直さないこと
    protected override void ResetToDefault(int wire, Array pointer)
    {
        ((float[])pointer)[0] = defaultValue;
    }

    public override void OnNetChangedWriter(int wire, Array pointer)
    {
        this.pointer = (float[])pointer;
        this.pointer[0] = valueToWrite;
    }

    public override void OnNetChangedReader(int wire, Array pointer)
    {
        this.pointer = (float[])pointer;
    }
}
```

値を読むときは、受け取った `pointer` を直接読みます。クラスのメンバを辿らないので、毎フレーム読んでも負荷が小さく済みます。

### オーバーライドできるメソッド

| メソッド | 呼ばれるとき・役割 |
|---|---|
| `CreateSourcePointers()` | 初めて必要になったときに1回。Wire ごとの配列を作る。機器がつながっていない Wire は `null` にする |
| `IsWriter(wire)` | その Wire が Writer かどうかを返す |
| `ResetToDefault(wire, pointer)` | Net の配り直しの最初に1回。`pointer` の要素をデフォルト値で上書きする |
| `OnNetChangedWriter(wire, pointer)` | Writer への通知。参照を差し替えて、すぐ書き込む |
| `OnNetChangedReader(wire, pointer)` | Reader への通知。参照を差し替える |
| `PostNetChanged(wire)` | Net が確定し、全員の通知が終わったあと |
| `WantsWrittenNotification(wire)` | 書き込みの通知を受けるかどうかを返す（下記） |
| `OnWritten(wire)` | 書き込みの通知を受けたとき（下記） |

### 気をつけること

- **通知のたびに参照を差し替える:** 通知で渡される `pointer` は、Net が変わると別の配列になることがあります。
- **配列を作り直さない:** 作り直すと、ほかの Reader が古い配列を見続けます。要素だけを書き換えます。
- **Writer は1つの Net に1つ:** 複数あるとどちらの値が残るかは決まっていません。
- **同じ Net の Wire は同じ型にする:** 型の違う配列が同じ Net に来ないように配線してください。
- **`IsWriter` を変えたら配り直す:** 書き込みをやめたときなどは、`RequestRefresh(wire)` を呼びます。
- **接続を変えるのは `PostNetChanged` の中で:** `OnNetChangedWriter` は Net の探索の途中で呼ばれるので、そこで `Connect` / `_Disconnect` を呼ばないでください。
- **`OnNetChanged〜` の中では引数の `pointer` を使う:** この時点で `currentPointers` はまだ古いままです。
- **配列を作っていない Wire には通知が届かない:** `IsWriter` が true でも同じです。

### 書き込みの通知

Reader が毎フレーム読まなくても、Writer が書いたことを知る仕組みです。ランプやリレーのように、たまにしか変わらない値に向いています。毎フレーム変わる値は、Reader が Update で読むほうが軽く済みます。

```csharp
// Writer 側: 書いたあとに呼ぶ。配列に書くだけでは通知されない
pointer[0] = value;
NotifyWritten(0);

// Reader 側: 希望した Wire にだけ届く
public override bool WantsWrittenNotification(int wire) { return true; }

public override void OnWritten(int wire)
{
    // pointer を読む
}
```

- 通知の相手は配り直しのときに決まります。`WantsWrittenNotification` の結果をあとから変えたら、`RequestRefresh(wire)` を呼んでください。
- Writer には届きません。
- 1回の `NotifyWritten` で、一覧の Reader に `OnWritten` が1回ずつ呼ばれます。配線が輪になっていても、それだけでは繰り返しません。
- `OnWritten` の中で別の Wire に書いて `NotifyWritten` を呼ぶこともできます。その連鎖が元の Wire に戻ってくると止まらなくなるので、そうならないようにしてください。

## 配線の例

### クロス配線

ケーブルの両端の Terminal で、割り当てを変えます。

```
一方の端: pinToWireMap = { 0, 1, 2 }
もう一方: pinToWireMap = { 2, 1, 0 }
```

### 分岐（Y 字ケーブル）

1つの Cable に Terminal を3つ置き、それぞれに同じ Wire を割り当てます。

### スイッチ

同じ Cable の Terminal 2つを `Connect` / `_Disconnect` すると、Wire どうしをつないだり切ったりできます。

```csharp
a.pinToWireMap = new int[] { 0 }; // Wire 0 側
b.pinToWireMap = new int[] { 1 }; // Wire 1 側

a.Connect(b);   // スイッチを入れる: Wire 0 と Wire 1 が同じ Net になる
a._Disconnect(); // スイッチを切る
```

多極スイッチは Pin を増やします。切り替えスイッチは、共通側の Terminal 1つと接点側の Terminal 2つをつなぎ替えます。

### リレー

通知を受けて接点を動かすときは、`PostNetChanged` の中で `Connect` / `_Disconnect` を呼びます。リレーどうしがフィードバックすると止まらなくなることがあるので、配線で避けてください。

## 接続できるプラグを制限する

`SocketCatchCollider` の `validationPickup` と、`PlugPickup` の `getPlugTypeName` をオーバーライドします。

```csharp
public class MyPlug : PlugPickup
{
    public override string getPlugTypeName()
    {
        return "Jumper";
    }
}

public class MySocket : SocketCatchCollider
{
    public override bool validationPickup(LUPickUpRC_RootChangeable pickup)
    {
        if (!base.validationPickup(pickup)) return false;
        return pickup.GetComponent<PlugPickup>().getPlugTypeName() == "Jumper";
    }
}
```

## デバッグ

Cable の `_DebugLogCurrentPointers()` を呼ぶと、Wire ごとの疑似ポインタの内容をログに出します。hash が同じ Wire は、同じ配列を共有しています。

つながるはずの Wire の値が揃わないときは、まず `pinToWireMap` が設定されているかを確認してください。

## 動作確認の状況

| 内容 | エディタのテスト | Udon 上 |
|---|---|---|
| 芯が1本のケーブル | 済み | 済み |
| 多芯（型の混在）、クロス配線、Pin の数が違う相手 | 済み | 済み |
| 分岐、スイッチ、リレー | 済み | 未確認 |
| 書き込みの通知 | 済み | 未確認 |
| 接続できるプラグの制限 | なし | 未確認 |

## 今後の予定

- 無性コネクタの表示（接触したコネクタどうしの中点へ移動する表示器）
