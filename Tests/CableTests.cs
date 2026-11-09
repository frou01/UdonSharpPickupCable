using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VRC.SDK3.Data;

namespace frou01.UdonSharpPickupCable.Tests
{
    /// <summary>
    /// Cable.RequestRefreshの探索（Netに含まれる(Cable, Wire)の集め方）のテスト。
    /// 接続はTerminal.Connectを使わずpartnerを直接書いて作る（Connect自体が探索を呼ぶため）。
    /// </summary>
    public class CableTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(nameof(CableTests));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        // ---- 基本 ----

        [Test]
        public void IsolatedWireReturnsOnlyStart()
        {
            Cable cable = CreateCable("Cable");
            CreateTerminal(cable, 0, 1);

            AssertNet(Search(cable, 0), (cable, 0));
        }

        [Test]
        public void UnconnectedTerminalIsIgnored()
        {
            Cable cable = CreateCable("Cable");
            CreateTerminal(cable, 0);
            CreateTerminal(cable, 0);

            AssertNet(Search(cable, 0), (cable, 0));
        }

        [Test]
        public void TwoCablesAreJoinedThroughSamePin()
        {
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0), CreateTerminal(socketCable, 0));

            AssertNet(Search(plugCable, 0), (plugCable, 0), (socketCable, 0));
        }

        [Test]
        public void PinMapsToPartnerWireByPinNumber()
        {
            // Pin番号が同じもの同士がつながる。Wire番号は両側で違ってよい。
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0, 1), CreateTerminal(socketCable, 1, 0));

            AssertNet(Search(plugCable, 0), (plugCable, 0), (socketCable, 1));
            AssertNet(Search(plugCable, 1), (plugCable, 1), (socketCable, 0));
        }

        [Test]
        public void OtherWiresOfSameTerminalAreNotIncluded()
        {
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0, 1, 2, 3), CreateTerminal(socketCable, 0, 1, 2, 3));

            AssertNet(Search(plugCable, 2), (plugCable, 2), (socketCable, 2));
        }

        [Test]
        public void SearchFromEitherSideGivesSameNet()
        {
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0), CreateTerminal(socketCable, 0));

            AssertNet(Search(socketCable, 0), (socketCable, 0), (plugCable, 0));
        }

        // ---- 対応するPinがない ----

        [Test]
        public void PinBeyondPartnerLengthIsSkipped()
        {
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0, 1, 2), CreateTerminal(socketCable, 0));

            AssertNet(Search(plugCable, 2), (plugCable, 2));
            AssertNet(Search(plugCable, 0), (plugCable, 0), (socketCable, 0));
        }

        [Test]
        public void SearchFromShorterSideDoesNotThrow()
        {
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0, 1, 2), CreateTerminal(socketCable, 0));

            AssertNet(Search(socketCable, 0), (socketCable, 0), (plugCable, 0));
        }

        [Test]
        public void PartnerPinWithoutWireIsSkipped()
        {
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0, 1), CreateTerminal(socketCable, 0, -1));

            AssertNet(Search(plugCable, 1), (plugCable, 1));
        }

        // ---- 分岐・複数Pin ----

        [Test]
        public void BranchingCableReachesAllBranches()
        {
            // 1本のCableの両端と途中の分岐にTerminalがある
            Cable trunkCable = CreateCable("Trunk");
            Cable leftCable = CreateCable("Left");
            Cable rightCable = CreateCable("Right");
            Cable branchCable = CreateCable("Branch");
            Mate(CreateTerminal(trunkCable, 0, 1), CreateTerminal(leftCable, 0, 1));
            Mate(CreateTerminal(trunkCable, 0, 1), CreateTerminal(rightCable, 0, 1));
            Mate(CreateTerminal(trunkCable, 1), CreateTerminal(branchCable, 0));

            AssertNet(Search(leftCable, 1), (leftCable, 1), (trunkCable, 1), (rightCable, 1), (branchCable, 0));
            AssertNet(Search(leftCable, 0), (leftCable, 0), (trunkCable, 0), (rightCable, 0));
        }

        [Test]
        public void WireOnSeveralPinsIsCollectedOnce()
        {
            // 同じWireを複数のPinに割り当て、相手側も同じWireで受ける
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Mate(CreateTerminal(plugCable, 0, 0, 0), CreateTerminal(socketCable, 0, 0, 0));

            AssertNet(Search(plugCable, 0), (plugCable, 0), (socketCable, 0));
        }

        [Test]
        public void ChainOfCablesIsFollowedToTheEnd()
        {
            Cable[] cables = new Cable[5];
            for (int i = 0; i < cables.Length; i++) cables[i] = CreateCable("Cable" + i);
            for (int i = 0; i + 1 < cables.Length; i++)
            {
                Mate(CreateTerminal(cables[i], 0), CreateTerminal(cables[i + 1], 0));
            }

            AssertNet(Search(cables[2], 0),
                (cables[0], 0), (cables[1], 0), (cables[2], 0), (cables[3], 0), (cables[4], 0));
        }

        [Test]
        public void UnreachableCablesAreNotIncluded()
        {
            Cable plugCable = CreateCable("Plug");
            Cable socketCable = CreateCable("Socket");
            Cable otherPlugCable = CreateCable("OtherPlug");
            Cable otherSocketCable = CreateCable("OtherSocket");
            Mate(CreateTerminal(plugCable, 0), CreateTerminal(socketCable, 0));
            Mate(CreateTerminal(otherPlugCable, 0), CreateTerminal(otherSocketCable, 0));

            AssertNet(Search(plugCable, 0), (plugCable, 0), (socketCable, 0));
        }

        // ---- ループ ----

        [Test]
        public void LoopOfCablesTerminatesWithoutDuplicates()
        {
            // 両端のTerminalを2本ともつないで輪にする
            Cable firstCable = CreateCable("First");
            Cable secondCable = CreateCable("Second");
            Mate(CreateTerminal(firstCable, 0), CreateTerminal(secondCable, 0));
            Mate(CreateTerminal(firstCable, 0), CreateTerminal(secondCable, 0));

            AssertNet(Search(firstCable, 0), (firstCable, 0), (secondCable, 0));
        }

        [Test]
        public void TerminatorBridgesTwoWiresOfSameCable()
        {
            // ターミネーターがPin 0とPin 1を自分のWire 0でつなぐ
            Cable mainCable = CreateCable("Main");
            Cable terminatorCable = CreateCable("Terminator");
            Mate(CreateTerminal(mainCable, 0, 1), CreateTerminal(terminatorCable, 0, 0));

            AssertNet(Search(mainCable, 0), (mainCable, 0), (terminatorCable, 0), (mainCable, 1));
            AssertNet(Search(mainCable, 1), (mainCable, 1), (terminatorCable, 0), (mainCable, 0));
        }

        [Test]
        public void SwitchConnectsTwoTerminalsOfSameCable()
        {
            // スイッチ: 同じCableのTerminal同士をつなぐ
            Cable switchCable = CreateCable("Switch");
            Terminal inTerminal = CreateTerminal(switchCable, 0);
            Terminal outTerminal = CreateTerminal(switchCable, 1);

            AssertNet(Search(switchCable, 0), (switchCable, 0));

            Mate(inTerminal, outTerminal);
            AssertNet(Search(switchCable, 0), (switchCable, 0), (switchCable, 1));
        }

        [Test]
        public void SwitchJoinsCablesOnBothSides()
        {
            Cable leftCable = CreateCable("Left");
            Cable switchCable = CreateCable("Switch");
            Cable rightCable = CreateCable("Right");
            Mate(CreateTerminal(leftCable, 0), CreateTerminal(switchCable, 0));
            Mate(CreateTerminal(switchCable, 1), CreateTerminal(rightCable, 0));
            Terminal inTerminal = CreateTerminal(switchCable, 0);
            Terminal outTerminal = CreateTerminal(switchCable, 1);

            AssertNet(Search(leftCable, 0), (leftCable, 0), (switchCable, 0));

            Mate(inTerminal, outTerminal);
            AssertNet(Search(leftCable, 0), (leftCable, 0), (switchCable, 0), (switchCable, 1), (rightCable, 0));
        }

        // ---- ヘルパー ----

        private Cable CreateCable(string name)
        {
            GameObject cableObject = new GameObject(name);
            cableObject.transform.SetParent(root.transform);
            Cable cable = cableObject.AddComponent<Cable>();
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

        private static List<(Cable, int)> Search(Cable start, int wire)
        {
            DataList result = start.RequestRefresh(wire);
            List<(Cable, int)> nodes = new List<(Cable, int)>();
            for (int i = 0; i < result.Count; i++)
            {
                DataList node = result[i].DataList;
                nodes.Add(((Cable)node[0].Reference, node[1].Int));
            }
            return nodes;
        }

        /// <summary>
        /// 順番は問わず、重複も許さずに比べる。
        /// </summary>
        private static void AssertNet(List<(Cable, int)> actual, params (Cable, int)[] expected)
        {
            Assert.That(actual, Is.EquivalentTo(expected), "Net = " + Describe(actual));
        }

        private static string Describe(List<(Cable, int)> nodes)
        {
            List<string> names = new List<string>();
            foreach ((Cable cable, int wire) in nodes) names.Add("(" + cable.name + ", " + wire + ")");
            return "[" + string.Join(", ", names) + "]";
        }
    }
}
