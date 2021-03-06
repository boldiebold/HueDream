﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HueDream.Models.Util;
using Microsoft.AspNetCore.Server.IIS.Core;
using Newtonsoft.Json;
using Q42.HueApi;
using Q42.HueApi.Models.Groups;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Models;

namespace HueDream.Models.Hue {
    public static class StreamingSetup {

        public static StreamingHueClient GetClient(BridgeData b) {
            var hueIp = b.Ip;
            var hueUser = b.User;
            var hueKey = b.Key;
            Console.WriteLine(@"Hue: Creating client...");
            //Initialize streaming client
            var client = new StreamingHueClient(hueIp, hueUser, hueKey);
            return client;
        }
        
        public static async Task StopStream(StreamingHueClient client, BridgeData b) {
            var id = b.SelectedGroup;
            Console.WriteLine($@"Hue: Stopping stream.");
            await client.LocalHueClient.SetStreamingAsync(id, false).ConfigureAwait(true);
        }

        public static async Task<StreamingGroup> SetupAndReturnGroup(StreamingHueClient client, BridgeData b, CancellationToken ct) {
            
            var groupId = b.SelectedGroup;
            Console.WriteLine(@"Hue: Created client.");
            //Get the entertainment group
            var group = client.LocalHueClient.GetGroupAsync(groupId).Result;
            if (group == null) {
                LogUtil.Write("Group is null, defaulting to first group...");
                var groups = b.GetGroups();
                if (groups.Count > 0) {
                    groupId = groups[0].Id;
                    group = client.LocalHueClient.GetGroupAsync(groupId).Result;
                    if (group != null) {
                        LogUtil.Write(@$"Selected first group: {groupId}");
                    }
                    else {
                        LogUtil.Write(@"Unable to load group, can't connect for streaming.");
                        return null;
                    }
                }
            }
            else {
                LogUtil.Write(@$"Group {groupId} fetched successfully.");
            }
            
            //Create a streaming group
            if (group != null) {
                var lights = group.Lights;
                Console.WriteLine(@"Group Lights: " + JsonConvert.SerializeObject(lights));
                var mappedLights =
                    (from light in lights from ml in b.Lights where ml.Id == light && ml.TargetSector != -1 select light)
                    .ToList();
                Console.WriteLine(@"Using mapped lights for group: " + JsonConvert.SerializeObject(mappedLights));
                var stream = new StreamingGroup(mappedLights);
                //Connect to the streaming group
                try {
                    LogUtil.Write(@"Connecting to client: " + group.Id);
                    await client.Connect(group.Id).ConfigureAwait(true);
                }
                catch (Exception e) {
                    LogUtil.Write(@"Exception: " + e);
                }

                LogUtil.Write(@"Client connected?");
                //Start auto updating this entertainment group
#pragma warning disable 4014
                client.AutoUpdate(stream, ct);
#pragma warning restore 4014

                //Optional: Check if streaming is currently active
                var bridgeInfo = await client.LocalHueClient.GetBridgeAsync().ConfigureAwait(true);
                LogUtil.Write(bridgeInfo.IsStreamingActive ? @"Streaming is active." : @"Streaming is not active.");
                return stream;
            }

            return null;
        }
        
    }
}