﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Wow.Helpers;

public static class Blacklist
{
    public static readonly HashSet<int> myBlacklist = new HashSet<int>
        {
            5134, //NPC died
            543,  //unable to generate path
            198,   //starter trainer without all spells or use minLevel in filter for this
            7952,
            23533,
            23603,
            23604,
            24494,
            24495,
            24501,
            26309,
            26328, //not prefered           
            26325, //bugged Hunter Trainer
            26332,
            26724,
            26758, //bugged warlocktrainer
            26738,
            26739,
            26740,
            26741,
            26742,
            26743,
            26744,
            26745,
            26746,
            26747,
            26748,
            26765,
            26749,
            26751,
            26752,
            26753,
            26754,
            26755,
            26756,
            26757,
            26759
        };
}