﻿using HueDream.Hue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueDream.HueDream {
    public class DreamSync {

        private HueBridge hueBridge;
        private DreamScreen.DreamScreen dreamScreen;
        private DataObj dreamData;
        private CancellationTokenSource cts;

        public static bool syncEnabled { get; set; }
        public DreamSync() {
            Console.WriteLine("Creating new sync.");
            dreamData = DreamData.LoadJson();
            hueBridge = new HueBridge(dreamData);
            dreamScreen = new DreamScreen.DreamScreen(this, dreamData);
            // Start our dreamscreen listening like a good boy
            if (!DreamScreen.DreamScreen.listening) {
                Console.WriteLine("DS Listen start.");
                dreamScreen.Listen();
                DreamScreen.DreamScreen.listening = true;
                Console.WriteLine("DS Listen running.");
            }
        }


        public void startSync() {
            cts = new CancellationTokenSource();
            Console.WriteLine("Starting sync.");
            dreamScreen.subscribe();
            Task.Run(async () => SyncData());
            Task.Run(async () => hueBridge.StartStream(cts.Token));
            Console.WriteLine("Sync should be running.");
            syncEnabled = true;
        }

        public void StopSync() {
            Console.WriteLine("Dreamsync: Stopsync fired.");
            cts.Cancel();
            syncEnabled = false;
        }

        private void SyncData() {
            hueBridge.setColors(dreamScreen.colors);
        }

        public void CheckSync(bool enabled) {
            if (dreamData.DsIp != "0.0.0.0" && enabled && !syncEnabled) {
                Console.WriteLine("Beginning DS stream to Hue...");
                Task.Run(() => startSync());
            } else if (!enabled && syncEnabled) {
                Console.WriteLine("Stopping sync.");
                StopSync();
            }
        }
    }
}
