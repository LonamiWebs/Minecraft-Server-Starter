// Made by Lonami Exo (C) LonamiWebs
// Creation date: february 2016
// Modifications:
// - No modifications made
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Minecraft_Server_Starter
{
    public partial class App : Application
    {
        public App()
        {
            SelectCulture(Thread.CurrentThread.CurrentCulture.ToString());

            

            

            

            

            

            

            

            

            

            

            

            

            

            

            

            

            

            

            InitializeComponent();

            SelectCulture(Thread.CurrentThread.CurrentUICulture.ToString());
            
            Settings.Init("LonamiWebs\\Minecraft Server Starter", new Dictionary<string, dynamic>
            {
                { "eulaAccepted", false },
                { "minRam", 512 },
                { "maxRam", 1024 },
                { "javaPath", Java.FindJavaPath() },
                { "priority", (int)ProcessPriorityClass.Normal },
                { "notificationEnabled", true },
                { "notificationLoc", (int)Toast.Location.TopLeft },
                { "mssFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                            "LonamiWebs\\Minecraft Server Starter")},
                { "ignoreCommandsBlock", true }
            });
        }

        public static void SelectCulture(string culture)
        {
            // List all our resources      
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Current.Resources.MergedDictionaries)
                dictionaryList.Add(dictionary);

            // We want our specific culture      
            string requestedCulture = string.Format("Strings.{0}.xaml", culture);
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            if (resourceDictionary == null)
            {
                requestedCulture = "Strings.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            }

            // If we have the requested resource, remove it from the list and place at the end.\      
            // Then this language will be our string table to use.      
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
            // Inform the threads of the new culture      
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }
    }
}














































