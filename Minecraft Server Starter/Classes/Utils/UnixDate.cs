/// <copyright file="UnixDate.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to convert between DateTime to unix time and back</summary>

using System;
using System.Globalization;

namespace Minecraft_Server_Starter
{
    public static class UnixDate
    {
        // epoch datetime
        static readonly DateTime epoch = new DateTime(1970, 1, 1);

        // unix time utils
        public static long DateTimeToUnixTime(DateTime dt)
        { return Convert.ToInt64((dt - epoch).TotalMilliseconds); }

        public static DateTime UnixTimeToDateTime(long ut)
        { return epoch.Add(TimeSpan.FromMilliseconds(ut)); }

        public static string GetUniversalDate()
        {
            CultureInfo tmp = CultureInfo.DefaultThreadCurrentCulture;

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            var result = DateTime.Now.ToString("ddd MMM dd HH:mm:ss CET yyyy");
            CultureInfo.DefaultThreadCurrentCulture = tmp;

            return result;
        }
    }
}
