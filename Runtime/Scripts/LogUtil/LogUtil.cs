using UdonSharp;
using UnityEngine;

namespace frou01.UdonSharpPickupCable
{
    /// <summary>
    /// ログに「[パッケージ名@バージョン][クラス名] 関数名: メッセージ (ヒエラルキー上のパス)」を付けて出す。
    /// 関数名はリフレクションが使えないので、呼び出し側からnameofで渡す。
    /// 例: LogUtil.Warning(this, nameof(Connect), "Connection target is null.");
    /// パッケージ名はハードコードなので、このファイルはパッケージごとに置く。
    /// </summary>
    public static class LogUtil
    {
        // package.jsonのnameとversionに合わせる
        public const string PackageName = "com.frou01.uspc";
        public const string PackageVersion = "0.0.1";
        private const string PackageTag = "[" + PackageName + "@" + PackageVersion + "]";

        public static void Log(UdonSharpBehaviour context, string method, string message)
        {
            Debug.Log(Format(context, method, message), context);
        }

        public static void Warning(UdonSharpBehaviour context, string method, string message)
        {
            Debug.LogWarning(Format(context, method, message), context);
        }

        public static void Error(UdonSharpBehaviour context, string method, string message)
        {
            Debug.LogError(Format(context, method, message), context);
        }

        public static string Format(UdonSharpBehaviour context, string method, string message)
        {
            if (context == null)
            {
                return PackageTag + "[null] " + method + ": " + message;
            }
            return PackageTag + "[" + GetClassName(context) + "] " + method + ": " + message + " (" + GetHierarchyPath(context.transform) + ")";
        }

        /// <summary>
        /// 名前空間を除いたクラス名
        /// </summary>
        public static string GetClassName(UdonSharpBehaviour context)
        {
            string typeName = context.GetUdonTypeName();
            int dotIndex = typeName.LastIndexOf('.');
            return dotIndex < 0 ? typeName : typeName.Substring(dotIndex + 1);
        }

        /// <summary>
        /// ルートからのパス（/Root/Child/Target）
        /// </summary>
        public static string GetHierarchyPath(Transform target)
        {
            string path = "/" + target.name;
            Transform current = target.parent;
            while (current != null)
            {
                path = "/" + current.name + path;
                current = current.parent;
            }
            return path;
        }
    }
}
