using System.Collections.Generic;
using frou01.util.editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace frou01.UdonSharpPickupCable.Editor
{
    /// <summary>
    /// 各Cableのterminalsを、そのCableを参照しているTerminalの一覧で上書きする。
    /// 順番はシーンのヒエラルキー順。非アクティブのものも含む。
    /// </summary>
    public class CableBuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => -300;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Dictionary<Cable, List<Terminal>> terminalsOfCables = new Dictionary<Cable, List<Terminal>>();
            foreach (Cable cable in BuildProcessUtility.FindComponents<Cable>(scene))
            {
                terminalsOfCables[cable] = new List<Terminal>();
            }

            foreach (Terminal terminal in BuildProcessUtility.FindComponents<Terminal>(scene))
            {
                if (terminal.cable == null)
                {
                    Debug.LogWarning($"[CableBuildProcess] {terminal.name} : cable is not set. Skipped", terminal);
                    continue;
                }
                if (!terminalsOfCables.TryGetValue(terminal.cable, out List<Terminal> terminals))
                {
                    Debug.LogWarning($"[CableBuildProcess] {terminal.name} : {terminal.cable.name} is not in this scene. Skipped", terminal);
                    continue;
                }
                terminals.Add(terminal);
            }

            foreach (KeyValuePair<Cable, List<Terminal>> pair in terminalsOfCables)
            {
                pair.Key.terminals = pair.Value.ToArray();
            }
        }
    }
}
