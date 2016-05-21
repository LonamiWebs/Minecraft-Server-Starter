/// <copyright file="BackupSort.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to sort backups</summary>

using System.Collections.Generic;

namespace Minecraft_Server_Starter
{
    class BackupSort : IComparer<Backup>
    {
        string priorityName;

        public BackupSort(string priorityName = null)
        {
            this.priorityName = priorityName;
        }

        public int Compare(Backup x, Backup y)
        {
            // if a priority name exists
            if (!string.IsNullOrEmpty(priorityName))
            {
                // and they have different names
                if (x.Server.Name != y.Server.Name)
                {
                    // and one is equal to the priority name, return that first
                    if (x.Server.Name.Equals(priorityName))
                        return -1;

                    if (y.Server.Name.Equals(priorityName))
                        return +1;
                }
            }

            // return the most recent one
            if (x.CreationDate > y.CreationDate)
                return -1;
            else
                return +1;
        }
    }
}
