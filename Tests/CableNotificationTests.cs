using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WireRole = frou01.UdonSharpPickupCable.Tests.FakeCable.WireRole;

namespace frou01.UdonSharpPickupCable.Tests
{
    /// <summary>
    /// Cable.RequestRefreshの後半（疑似ポインタの割り当てと通知）のテスト。
    /// </summary>
    public class CableNotificationTests
    {
        private GameObject root;
        private List<string> eventLog;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(nameof(CableNotificationTests));
            eventLog = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        // ---- 配列の共有 ----

        [Test]
        public void WriterAndReadersReceiveSameArray()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable firstReader = CreateFake("FirstReader", WireRole.Reader);
            FakeCable secondReader = CreateFake("SecondReader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(firstReader, 0));
            Mate(CreateTerminal(firstReader, 0), CreateTerminal(secondReader, 0));

            writer.RequestRefresh(0);

            Assert.That(firstReader.receivedPointers[0], Is.SameAs(writer.receivedPointers[0]));
            Assert.That(secondReader.receivedPointers[0], Is.SameAs(writer.receivedPointers[0]));
        }

        [Test]
        public void ArrayPassesThroughPlainCable()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            Cable plain = CreatePlainCable("Plain");
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(plain, 0));
            Mate(CreateTerminal(plain, 0), CreateTerminal(reader, 0));

            writer.RequestRefresh(0);

            Assert.That(reader.receivedPointers[0], Is.SameAs(writer.receivedPointers[0]));
        }

        [Test]
        public void PlainCableAtStartDoesNotBlockDistribution()
        {
            Cable plain = CreatePlainCable("Plain");
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(plain, 0), CreateTerminal(writer, 0));
            Mate(CreateTerminal(plain, 0), CreateTerminal(reader, 0));

            plain.RequestRefresh(0);

            Assert.That(writer.receivedPointers[0], Is.Not.Null);
            Assert.That(reader.receivedPointers[0], Is.SameAs(writer.receivedPointers[0]));
        }

        [Test]
        public void AllMembersGetCurrentPointer()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(reader, 0));

            writer.RequestRefresh(0);

            Assert.That(writer.GetCurrentPointer(0), Is.SameAs(writer.receivedPointers[0]));
            Assert.That(reader.GetCurrentPointer(0), Is.SameAs(writer.receivedPointers[0]));
        }

        [Test]
        public void WiresOfDifferentNetsUseDifferentArrays()
        {
            FakeCable plug = CreateFake("Plug", WireRole.Writer, WireRole.Writer);
            FakeCable socket = CreateFake("Socket", WireRole.Reader, WireRole.Reader);
            Mate(CreateTerminal(plug, 0, 1), CreateTerminal(socket, 0, 1));

            plug.RequestRefresh(0);
            plug.RequestRefresh(1);

            Assert.That(socket.receivedPointers[0], Is.SameAs(plug.receivedPointers[0]));
            Assert.That(socket.receivedPointers[1], Is.SameAs(plug.receivedPointers[1]));
            Assert.That(plug.receivedPointers[0], Is.Not.SameAs(plug.receivedPointers[1]));
        }

        [Test]
        public void TerminatorSharesArrayBetweenTwoWiresOfSameCable()
        {
            FakeCable main = CreateFake("Main", WireRole.Reader, WireRole.Reader);
            Cable terminator = CreatePlainCable("Terminator");
            Mate(CreateTerminal(main, 0, 1), CreateTerminal(terminator, 0, 0));

            main.RequestRefresh(0);

            Assert.That(main.receivedPointers[1], Is.SameAs(main.receivedPointers[0]));
        }

        // ---- 順番 ----

        [Test]
        public void ResetThenWritersThenReadersEvenWhenStartingFromReader()
        {
            // 探索の順番ではReaderが先になるので、通知を分けていないと順番が崩れる
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            Mate(CreateTerminal(reader, 0), CreateTerminal(writer, 0));

            reader.RequestRefresh(0);

            Assert.That(eventLog, Is.EqualTo(new[]
            {
                "Reset:Reader:0", "Writer:Writer:0", "Reader:Reader:0", "Post:Reader:0", "Post:Writer:0",
            }));
        }

        [Test]
        public void ResetRunsOnceEvenWhenSeveralMembersHaveArrays()
        {
            FakeCable firstReader = CreateFake("FirstReader", WireRole.Reader);
            FakeCable secondReader = CreateFake("SecondReader", WireRole.Reader);
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            Mate(CreateTerminal(firstReader, 0), CreateTerminal(secondReader, 0));
            Mate(CreateTerminal(secondReader, 0), CreateTerminal(writer, 0));

            firstReader.RequestRefresh(0);

            Assert.That(eventLog.FindAll(entry => entry.StartsWith("Reset")).Count, Is.EqualTo(1));
        }

        [Test]
        public void EachMemberIsNotifiedOnce()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(reader, 0));

            writer.RequestRefresh(0);

            Assert.That(eventLog.FindAll(entry => entry == "Writer:Writer:0").Count, Is.EqualTo(1));
            Assert.That(eventLog.FindAll(entry => entry == "Reader:Reader:0").Count, Is.EqualTo(1));
        }

        // ---- PostNetChanged ----

        [Test]
        public void PostRunsOnceForEachWriterAndReader()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(reader, 0));

            writer.RequestRefresh(0);

            Assert.That(eventLog.FindAll(entry => entry == "Post:Writer:0").Count, Is.EqualTo(1));
            Assert.That(eventLog.FindAll(entry => entry == "Post:Reader:0").Count, Is.EqualTo(1));
        }

        [Test]
        public void PostSeesUpdatedCurrentPointerAndWrittenValue()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.valueToWrite = 100f;
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(reader, 0));

            reader.RequestRefresh(0);

            Assert.That(reader.currentPointersAtPost[0], Is.SameAs(writer.receivedPointers[0]));
            Assert.That(reader.valuesAtPost[0], Is.EqualTo(100f));
        }

        [Test]
        public void ConnectInsidePostJoinsOtherNet()
        {
            // リレー: Wire 0がコイル、Wire 1とWire 2が接点。コイルの通知で接点をつなぐ
            FakeCable coilDriver = CreateFake("CoilDriver", WireRole.Writer);
            FakeCable relay = CreateFake("Relay", WireRole.Reader, WireRole.None, WireRole.None);
            FakeCable contactWriter = CreateFake("ContactWriter", WireRole.Writer);
            contactWriter.valueToWrite = 100f;
            FakeCable contactReader = CreateFake("ContactReader", WireRole.Reader);
            Mate(CreateTerminal(coilDriver, 0), CreateTerminal(relay, 0));
            Mate(CreateTerminal(contactWriter, 0), CreateTerminal(relay, 1));
            Mate(CreateTerminal(relay, 2), CreateTerminal(contactReader, 0));
            relay.terminalToConnectOnPost = CreateTerminal(relay, 1);
            relay.connectTargetOnPost = CreateTerminal(relay, 2);

            coilDriver.RequestRefresh(0);

            Assert.That(ReadValue(contactReader, 0), Is.EqualTo(100f));
            Assert.That(relay.receivedPointers[0], Is.SameAs(coilDriver.receivedPointers[0]));
            Assert.That(relay.receivedPointers[0], Is.Not.SameAs(contactReader.receivedPointers[0]));
        }

        // ---- 値 ----

        [Test]
        public void ReaderSeesValueWrittenByWriter()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.valueToWrite = 100f;
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(reader, 0));

            reader.RequestRefresh(0);

            Assert.That(ReadValue(reader, 0), Is.EqualTo(100f));
        }

        [Test]
        public void NetWithoutWriterHoldsDefaultValue()
        {
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            reader.defaultValue = 5f;

            reader.RequestRefresh(0);

            Assert.That(ReadValue(reader, 0), Is.EqualTo(5f));
        }

        [Test]
        public void ValueReturnsToDefaultWhenWriterStops()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.valueToWrite = 100f;
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(reader, 0));
            writer.RequestRefresh(0);

            writer.SetWriter(0, false);
            writer.RequestRefresh(0);

            Assert.That(ReadValue(reader, 0), Is.EqualTo(0f));
        }

        [Test]
        public void ArrayIsNotRecreatedByRefresh()
        {
            FakeCable reader = CreateFake("Reader", WireRole.Reader);

            reader.RequestRefresh(0);
            Array first = reader.receivedPointers[0];
            reader.RequestRefresh(0);

            Assert.That(reader.receivedPointers[0], Is.SameAs(first));
        }

        // ---- 配列を持つメンバーがいない ----

        [Test]
        public void NoNotificationWhenNetHasNoArray()
        {
            FakeCable fake = CreateFake("Fake", WireRole.None);
            Cable plain = CreatePlainCable("Plain");
            Mate(CreateTerminal(fake, 0), CreateTerminal(plain, 0));

            plain.RequestRefresh(0);

            Assert.That(eventLog, Is.Empty);
            Assert.That(fake.GetCurrentPointer(0), Is.Null);
        }

        [Test]
        public void WireBeyondOwnArraysIsNotUsedAsSource()
        {
            FakeCable fake = CreateFake("Fake", WireRole.Reader);

            fake.RequestRefresh(1);

            Assert.That(eventLog, Is.Empty);
        }

        [Test]
        public void WireWithoutOwnArrayGetsCurrentPointerButNoNotification()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable fake = CreateFake("Fake", WireRole.None);
            Mate(CreateTerminal(writer, 0), CreateTerminal(fake, 0));

            fake.RequestRefresh(0);

            Assert.That(fake.GetCurrentPointer(0), Is.SameAs(writer.receivedPointers[0]));
            Assert.That(eventLog.FindAll(entry => entry.Contains(":Fake:")), Is.Empty);
        }

        // ---- Connect / _Disconnect ----

        [Test]
        public void ConnectSharesArrayAndPassesValue()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.valueToWrite = 100f;
            FakeCable reader = CreateFake("Reader", WireRole.Reader);

            CreateTerminal(writer, 0).Connect(CreateTerminal(reader, 0));

            Assert.That(reader.receivedPointers[0], Is.SameAs(writer.receivedPointers[0]));
            Assert.That(ReadValue(reader, 0), Is.EqualTo(100f));
        }

        [Test]
        public void DisconnectSeparatesArraysAndReaderReturnsToDefault()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.valueToWrite = 100f;
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Terminal writerTerminal = CreateTerminal(writer, 0);
            writerTerminal.Connect(CreateTerminal(reader, 0));

            writerTerminal._Disconnect();

            Assert.That(reader.receivedPointers[0], Is.Not.SameAs(writer.receivedPointers[0]));
            Assert.That(ReadValue(reader, 0), Is.EqualTo(0f));
            Assert.That(ReadValue(writer, 0), Is.EqualTo(100f));
        }

        [Test]
        public void DisconnectFromEitherSideGivesSameResult()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.valueToWrite = 100f;
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            Terminal readerTerminal = CreateTerminal(reader, 0);
            CreateTerminal(writer, 0).Connect(readerTerminal);

            readerTerminal._Disconnect();

            Assert.That(reader.receivedPointers[0], Is.Not.SameAs(writer.receivedPointers[0]));
            Assert.That(ReadValue(reader, 0), Is.EqualTo(0f));
            Assert.That(ReadValue(writer, 0), Is.EqualTo(100f));
        }

        [Test]
        public void ReconnectToAnotherPartnerUpdatesFormerPartner()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.valueToWrite = 100f;
            FakeCable formerReader = CreateFake("FormerReader", WireRole.Reader);
            FakeCable newReader = CreateFake("NewReader", WireRole.Reader);
            Terminal writerTerminal = CreateTerminal(writer, 0);
            writerTerminal.Connect(CreateTerminal(formerReader, 0));

            writerTerminal.Connect(CreateTerminal(newReader, 0));

            Assert.That(ReadValue(newReader, 0), Is.EqualTo(100f));
            Assert.That(ReadValue(formerReader, 0), Is.EqualTo(0f));
            Assert.That(formerReader.receivedPointers[0], Is.Not.SameAs(writer.receivedPointers[0]));
        }

        [Test]
        public void ConnectRefreshesEachAssignedWireOnce()
        {
            // 同じWireを複数のPinに割り当て、-1のPinも混ぜる
            FakeCable plug = CreateFake("Plug", WireRole.Reader, WireRole.Reader);
            FakeCable socket = CreateFake("Socket", WireRole.Reader, WireRole.Reader);

            CreateTerminal(plug, 0, 0, -1, 1).Connect(CreateTerminal(socket, 0, 0, -1, 1));

            Assert.That(eventLog.FindAll(entry => entry.StartsWith("Reset")).Count, Is.EqualTo(2));
            Assert.That(socket.receivedPointers[0], Is.SameAs(plug.receivedPointers[0]));
            Assert.That(socket.receivedPointers[1], Is.SameAs(plug.receivedPointers[1]));
        }

        [Test]
        public void DisconnectRefreshesEachAssignedWireOnce()
        {
            FakeCable plug = CreateFake("Plug", WireRole.Reader);
            FakeCable socket = CreateFake("Socket", WireRole.Reader);
            Terminal plugTerminal = CreateTerminal(plug, 0, 0, -1);
            plugTerminal.Connect(CreateTerminal(socket, 0, 0, -1));
            eventLog.Clear();

            plugTerminal._Disconnect();

            Assert.That(eventLog.FindAll(entry => entry == "Reset:Plug:0").Count, Is.EqualTo(1));
            Assert.That(eventLog.FindAll(entry => entry == "Reset:Socket:0").Count, Is.EqualTo(1));
        }

        [Test]
        public void TerminalWithOnlyEmptyPinsRefreshesNothing()
        {
            FakeCable plug = CreateFake("Plug", WireRole.Reader);
            FakeCable socket = CreateFake("Socket", WireRole.Reader);

            CreateTerminal(plug, -1).Connect(CreateTerminal(socket, 0));

            Assert.That(eventLog, Is.Empty);
        }

        // ---- NotifyWritten ----

        [Test]
        public void NotifyWrittenReachesOnlyReadersThatWantIt()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable wantingReader = CreateFake("WantingReader", WireRole.Reader);
            wantingReader.wantsWritten = true;
            FakeCable otherReader = CreateFake("OtherReader", WireRole.Reader);
            Mate(CreateTerminal(writer, 0), CreateTerminal(wantingReader, 0));
            Mate(CreateTerminal(wantingReader, 0), CreateTerminal(otherReader, 0));
            writer.RequestRefresh(0);
            eventLog.Clear();

            writer.WriteAndNotify(0, 200f);

            Assert.That(eventLog, Is.EqualTo(new[] { "Written:WantingReader:0" }));
            Assert.That(ReadValue(wantingReader, 0), Is.EqualTo(200f));
        }

        [Test]
        public void NotifyWrittenPassesWireNumberOfReader()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable reader = CreateFake("Reader", WireRole.None, WireRole.Reader);
            reader.wantsWritten = true;
            Mate(CreateTerminal(writer, 0), CreateTerminal(reader, 1));
            writer.RequestRefresh(0);
            eventLog.Clear();

            writer.WriteAndNotify(0, 200f);

            Assert.That(eventLog, Is.EqualTo(new[] { "Written:Reader:1" }));
        }

        [Test]
        public void NotifyWrittenDoesNotReachOtherNets()
        {
            FakeCable plug = CreateFake("Plug", WireRole.Writer, WireRole.Writer);
            FakeCable socket = CreateFake("Socket", WireRole.Reader, WireRole.Reader);
            socket.wantsWritten = true;
            Mate(CreateTerminal(plug, 0, 1), CreateTerminal(socket, 0, 1));
            plug.RequestRefresh(0);
            plug.RequestRefresh(1);
            eventLog.Clear();

            plug.WriteAndNotify(1, 200f);

            Assert.That(eventLog, Is.EqualTo(new[] { "Written:Socket:1" }));
        }

        [Test]
        public void NotifyWrittenBeforeFirstRefreshDoesNothing()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);

            writer.NotifyOnly(0);

            Assert.That(eventLog, Is.Empty);
        }

        [Test]
        public void NotifyWrittenForWireWithoutListenersDoesNothing()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.RequestRefresh(0);
            eventLog.Clear();

            writer.NotifyOnly(5);

            Assert.That(eventLog, Is.Empty);
        }

        [Test]
        public void NotifyWrittenInsidePostUsesNewListeners()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            writer.shouldNotifyOnPost = true;
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            reader.wantsWritten = true;

            CreateTerminal(writer, 0).Connect(CreateTerminal(reader, 0));

            Assert.That(eventLog.FindAll(entry => entry == "Written:Reader:0").Count, Is.EqualTo(1));
        }

        [Test]
        public void ListenersFollowConnectAndDisconnect()
        {
            FakeCable writer = CreateFake("Writer", WireRole.Writer);
            FakeCable reader = CreateFake("Reader", WireRole.Reader);
            reader.wantsWritten = true;
            Terminal writerTerminal = CreateTerminal(writer, 0);
            writer.RequestRefresh(0);
            eventLog.Clear();

            writer.NotifyOnly(0);
            Assert.That(eventLog, Is.Empty, "before connect");

            writerTerminal.Connect(CreateTerminal(reader, 0));
            eventLog.Clear();
            writer.NotifyOnly(0);
            Assert.That(eventLog, Is.EqualTo(new[] { "Written:Reader:0" }), "after connect");

            writerTerminal._Disconnect();
            eventLog.Clear();
            writer.NotifyOnly(0);
            Assert.That(eventLog, Is.Empty, "after disconnect");
        }

        // ---- ヘルパー ----

        private FakeCable CreateFake(string name, params WireRole[] wireRoles)
        {
            FakeCable cable = CreateCableObject<FakeCable>(name);
            cable.wireRoles = wireRoles;
            cable.eventLog = eventLog;
            return cable;
        }

        private Cable CreatePlainCable(string name)
        {
            return CreateCableObject<Cable>(name);
        }

        private T CreateCableObject<T>(string name) where T : Cable
        {
            GameObject cableObject = new GameObject(name);
            cableObject.transform.SetParent(root.transform);
            T cable = cableObject.AddComponent<T>();
            cable.terminals = new Terminal[0];
            return cable;
        }

        /// <summary>
        /// cableの子にTerminalを作り、cable.terminalsの末尾に加える。
        /// </summary>
        private static Terminal CreateTerminal(Cable cable, params int[] pinToWireMap)
        {
            GameObject terminalObject = new GameObject("Terminal" + cable.terminals.Length);
            terminalObject.transform.SetParent(cable.transform);
            Terminal terminal = terminalObject.AddComponent<Terminal>();
            terminal.cable = cable;
            terminal.pinToWireMap = pinToWireMap;

            Terminal[] terminals = new Terminal[cable.terminals.Length + 1];
            cable.terminals.CopyTo(terminals, 0);
            terminals[terminals.Length - 1] = terminal;
            cable.terminals = terminals;
            return terminal;
        }

        private static void Mate(Terminal terminal, Terminal other)
        {
            terminal.partner = other;
            other.partner = terminal;
        }

        private static float ReadValue(FakeCable cable, int wire)
        {
            return ((float[])cable.receivedPointers[wire])[0];
        }
    }

    /// <summary>
    /// テスト用のCable。Wireごとに役割を決めて、通知された内容を記録する。
    /// </summary>
    public class FakeCable : Cable
    {
        public enum WireRole
        {
            /// <summary>機器がつながっていない。自分の配列を持たない。</summary>
            None,
            Reader,
            Writer,
        }

        /// <summary>
        /// 通知の順番を確かめるための記録。複数のFakeCableで共有する。
        /// </summary>
        public List<string> eventLog;

        public WireRole[] wireRoles = new WireRole[0];
        public float defaultValue;
        public float valueToWrite;

        /// <summary>
        /// Wireごとに、最後に通知された疑似ポインタ
        /// </summary>
        public readonly Dictionary<int, Array> receivedPointers = new Dictionary<int, Array>();

        /// <summary>
        /// Wireごとに、PostNetChangedの時点でのcurrentPointersの中身
        /// </summary>
        public readonly Dictionary<int, object> currentPointersAtPost = new Dictionary<int, object>();

        /// <summary>
        /// Wireごとに、PostNetChangedの時点で配列から読んだ値
        /// </summary>
        public readonly Dictionary<int, float> valuesAtPost = new Dictionary<int, float>();

        /// <summary>
        /// 設定すると、最初のPostNetChangedでconnectTargetOnPostとつなぐ（リレーの再現）。
        /// </summary>
        public Terminal terminalToConnectOnPost;
        public Terminal connectTargetOnPost;

        public bool wantsWritten;

        /// <summary>
        /// trueなら、PostNetChangedの中で全Wire分のNotifyWrittenを呼ぶ。
        /// </summary>
        public bool shouldNotifyOnPost;

        public void WriteAndNotify(int wire, float value)
        {
            ((float[])receivedPointers[wire])[0] = value;
            NotifyWritten(wire);
        }

        public void NotifyOnly(int wire)
        {
            NotifyWritten(wire);
        }

        public void SetWriter(int wire, bool isWriter)
        {
            wireRoles[wire] = isWriter ? WireRole.Writer : WireRole.Reader;
        }

        /// <summary>
        /// thisのwireが自分用に持っている配列を返す。
        /// </summary>
        public float[] GetOwnArray(int wire)
        {
            return (float[])_GetSourcePointers()[wire];
        }

        /// <summary>
        /// thisのwireのcurrentPointersを返す。
        /// </summary>
        public object GetCurrentPointer(int wire)
        {
            return currentPointers[wire];
        }

        public override bool IsWriter(int wire)
        {
            return wire < wireRoles.Length && wireRoles[wire] == WireRole.Writer;
        }

        public override void OnNetChangedWriter(int wire, Array pointer)
        {
            Record("Writer", wire);
            receivedPointers[wire] = pointer;
            ((float[])pointer)[0] = valueToWrite;
        }

        public override void OnNetChangedReader(int wire, Array pointer)
        {
            Record("Reader", wire);
            receivedPointers[wire] = pointer;
        }

        public override void PostNetChanged(int wire)
        {
            Record("Post", wire);
            currentPointersAtPost[wire] = currentPointers[wire];
            valuesAtPost[wire] = ((float[])currentPointers[wire])[0];
            if (shouldNotifyOnPost) NotifyWritten(wire);

            if (terminalToConnectOnPost != null)
            {
                Terminal terminal = terminalToConnectOnPost;
                terminalToConnectOnPost = null;
                terminal.Connect(connectTargetOnPost);
            }
        }

        public override bool WantsWrittenNotification(int wire)
        {
            return wantsWritten;
        }

        public override void OnWritten(int wire)
        {
            Record("Written", wire);
        }

        protected override void ResetToDefault(int wire, Array pointer)
        {
            Record("Reset", wire);
            float[] values = (float[])pointer;
            for (int i = 0; i < values.Length; i++) values[i] = defaultValue;
        }

        protected override object[] CreateSourcePointers()
        {
            object[] pointers = new object[wireRoles.Length];
            for (int wire = 0; wire < wireRoles.Length; wire++)
            {
                if (wireRoles[wire] != WireRole.None) pointers[wire] = new float[1];
            }
            return pointers;
        }

        private void Record(string eventName, int wire)
        {
            if (eventLog != null) eventLog.Add(eventName + ":" + name + ":" + wire);
        }
    }
}
