// Made by Lonami Exo (C) LonamiWebs
// Creation date: february 2016
// Modifications:
// - No modifications made
using System.Windows;

namespace Minecraft_Server_Starter
{
    class Res
    {
        static readonly FrameworkElement context = new FrameworkElement();

        // note that this will NOT check against invalid names
        public static T GetRes<T>(string name)
        {
            return (T)context.FindResource(name);
        }

        // returns null if the resource was not found
        public static string GetStr(string name, params object[] args)
        {
            try {
                var result = GetRes<string>(name);
                return args.Length > 0 ? string.Format(result, args) : result;
            }
            catch { return null; }
        }
    }
}
