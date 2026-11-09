using System.Text.RegularExpressions;
using frou01.UdonSharpPickupCable.Editor;
using frou01.UdonSharpPickupCable.Samples;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace frou01.UdonSharpPickupCable.Tests
{
    /// <summary>
    /// CableBuildProcessのテスト。テストランナーが用意する無題のシーンを処理する（作業中のシーンは書き換えない）。
    /// </summary>
    public class CableBuildProcessTests
    {
        private Scene scene;
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            scene = SceneManager.GetActiveScene();
            Assert.That(scene.path, Is.Empty, "Expected the untitled scene of the test runner.");
            root = new GameObject(nameof(CableBuildProcessTests));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void TerminalAnywhereInSceneIsCollected()
        {
            Cable cable = CreateCable("Cable");
            Terminal child = CreateTerminal("Child", cable, cable.transform);
            Terminal outside = CreateTerminal("Outside", cable, root.transform);

            Process();

            Assert.That(cable.terminals, Is.EqualTo(new[] { child, outside }));
        }

        [Test]
        public void TerminalGoesToTheCableItRefersTo()
        {
            Cable firstCable = CreateCable("First");
            Cable secondCable = CreateCable("Second");
            Terminal firstTerminal = CreateTerminal("FirstTerminal", firstCable, secondCable.transform);
            Terminal secondTerminal = CreateTerminal("SecondTerminal", secondCable, firstCable.transform);

            Process();

            Assert.That(firstCable.terminals, Is.EqualTo(new[] { firstTerminal }));
            Assert.That(secondCable.terminals, Is.EqualTo(new[] { secondTerminal }));
        }

        [Test]
        public void InactiveTerminalIsCollected()
        {
            Cable cable = CreateCable("Cable");
            Terminal terminal = CreateTerminal("Terminal", cable, cable.transform);
            terminal.gameObject.SetActive(false);

            Process();

            Assert.That(cable.terminals, Is.EqualTo(new[] { terminal }));
        }

        [Test]
        public void ExistingEntriesAreOverwritten()
        {
            Cable cable = CreateCable("Cable");
            Cable otherCable = CreateCable("Other");
            Terminal terminal = CreateTerminal("Terminal", cable, cable.transform);
            Terminal otherTerminal = CreateTerminal("OtherTerminal", otherCable, otherCable.transform);
            cable.terminals = new[] { otherTerminal, null };

            Process();

            Assert.That(cable.terminals, Is.EqualTo(new[] { terminal }));
        }

        [Test]
        public void CableWithoutTerminalGetsEmptyArray()
        {
            Cable cable = CreateCable("Cable");
            cable.terminals = null;

            Process();

            Assert.That(cable.terminals, Is.Empty);
        }

        [Test]
        public void ProcessingTwiceDoesNotDuplicate()
        {
            Cable cable = CreateCable("Cable");
            Terminal terminal = CreateTerminal("Terminal", cable, cable.transform);

            Process();
            Process();

            Assert.That(cable.terminals, Is.EqualTo(new[] { terminal }));
        }

        [Test]
        public void TerminalWithoutCableIsSkippedWithWarning()
        {
            Cable cable = CreateCable("Cable");
            Terminal terminal = CreateTerminal("Terminal", cable, cable.transform);
            CreateTerminal("NoCable", null, root.transform);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[CableBuildProcess\] NoCable : cable is not set"));
            Process();

            Assert.That(cable.terminals, Is.EqualTo(new[] { terminal }));
        }

        [Test]
        public void DerivedCableIsCollected()
        {
            GameObject cableObject = new GameObject("Derived");
            cableObject.transform.SetParent(root.transform);
            SimpleFloatCable cable = cableObject.AddComponent<SimpleFloatCable>();
            Terminal terminal = CreateTerminal("Terminal", cable, cableObject.transform);

            Process();

            Assert.That(cable.terminals, Is.EqualTo(new[] { terminal }));
        }

        private void Process()
        {
            new CableBuildProcess().OnProcessScene(scene, null);
        }

        private Cable CreateCable(string name)
        {
            GameObject cableObject = new GameObject(name);
            cableObject.transform.SetParent(root.transform);
            return cableObject.AddComponent<Cable>();
        }

        private static Terminal CreateTerminal(string name, Cable cable, Transform parent)
        {
            GameObject terminalObject = new GameObject(name);
            terminalObject.transform.SetParent(parent);
            Terminal terminal = terminalObject.AddComponent<Terminal>();
            terminal.cable = cable;
            return terminal;
        }
    }
}
