using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace frou01.UdonSharpPickupCable.Tests
{
    /// <summary>
    /// SimpleFloatCableのテスト。
    /// </summary>
    public class SimpleFloatCableTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(nameof(SimpleFloatCableTests));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ReaderSeesWriterValueAfterConnect()
        {
            SimpleFloatCable writer = CreateCable("Writer", true, 100f);
            SimpleFloatCable reader = CreateCable("Reader", false, 0f);

            CreateTerminal(writer).Connect(CreateTerminal(reader));

            Assert.That(reader.pointer[0], Is.EqualTo(100f));
            Assert.That(reader.pointer, Is.SameAs(writer.pointer));
        }

        [Test]
        public void ReaderReturnsToDefaultAfterDisconnect()
        {
            SimpleFloatCable writer = CreateCable("Writer", true, 100f);
            SimpleFloatCable reader = CreateCable("Reader", false, 0f);
            reader.defaultValue = 5f;
            Terminal readerTerminal = CreateTerminal(reader);
            CreateTerminal(writer).Connect(readerTerminal);

            readerTerminal._Disconnect();

            Assert.That(reader.pointer[0], Is.EqualTo(5f));
            Assert.That(writer.pointer[0], Is.EqualTo(100f));
        }

        [Test]
        public void StoppingWriterReturnsNetToDefault()
        {
            SimpleFloatCable writer = CreateCable("Writer", true, 100f);
            SimpleFloatCable reader = CreateCable("Reader", false, 0f);
            CreateTerminal(writer).Connect(CreateTerminal(reader));

            writer.isWriter = false;
            writer.RequestRefresh(0);

            Assert.That(reader.pointer[0], Is.EqualTo(0f));
        }

        [Test]
        public void DebugLogShowsNullBeforeFirstRefresh()
        {
            SimpleFloatCable cable = CreateCable("Cable", false, 0f);

            LogAssert.Expect(LogType.Log, new Regex("currentPointers is null"));
            cable._DebugLogCurrentPointers();
        }

        [Test]
        public void DebugLogShowsValuesAndSameHashForSharedArray()
        {
            SimpleFloatCable writer = CreateCable("Writer", true, 100f);
            SimpleFloatCable reader = CreateCable("Reader", false, 0f);
            CreateTerminal(writer).Connect(CreateTerminal(reader));
            string hash = "hash=" + reader.pointer.GetHashCode() + @" values=\[100\]";

            LogAssert.Expect(LogType.Log, new Regex(hash));
            writer._DebugLogCurrentPointers();
            LogAssert.Expect(LogType.Log, new Regex(hash));
            reader._DebugLogCurrentPointers();
        }

        private SimpleFloatCable CreateCable(string name, bool isWriter, float valueToWrite)
        {
            GameObject cableObject = new GameObject(name);
            cableObject.transform.SetParent(root.transform);
            SimpleFloatCable cable = cableObject.AddComponent<SimpleFloatCable>();
            cable.terminals = new Terminal[0];
            cable.isWriter = isWriter;
            cable.valueToWrite = valueToWrite;
            return cable;
        }

        /// <summary>
        /// Pinが1つ（Wire 0）のTerminalを作り、cable.terminalsの末尾に加える。
        /// </summary>
        private static Terminal CreateTerminal(Cable cable)
        {
            GameObject terminalObject = new GameObject("Terminal" + cable.terminals.Length);
            terminalObject.transform.SetParent(cable.transform);
            Terminal terminal = terminalObject.AddComponent<Terminal>();
            terminal.cable = cable;
            terminal.pinToWire = new int[] { 0 };

            Terminal[] terminals = new Terminal[cable.terminals.Length + 1];
            cable.terminals.CopyTo(terminals, 0);
            terminals[terminals.Length - 1] = terminal;
            cable.terminals = terminals;
            return terminal;
        }
    }
}
