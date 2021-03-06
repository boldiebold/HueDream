﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using HueDream.Models.DreamScreen.Devices;
using HueDream.Models.Hue;
using JsonFlatFileDataStore;
using Newtonsoft.Json;

namespace HueDream.Models {
    [Serializable]
    public static class DreamData {
        public static DataStore GetStore() {
            var path = GetConfigPath("store.json");
            var createDefaults = !File.Exists(path);
            var store = new DataStore(path);
            return createDefaults ? SetDefaults(store) : store;
        }

        /// <summary>
        ///     Loads our data store from a dynamic path, and tries to get the item
        /// </summary>
        /// <param name="key"></param>
        /// <returns>dynamic object corresponding to key, or null if not found</returns>
        public static dynamic GetItem(string key) {
            try {
                using var dStore = new DataStore(GetConfigPath("store.json"));
                var output = dStore.GetItem(key);
                return output;
            }
            catch (KeyNotFoundException) { }

            return null;
        }

        private static DataStore SetDefaults(DataStore store) {
            store.InsertItem("dsIp", "0.0.0.0");
            BaseDevice myDevice = new SideKick(GetLocalIpAddress());
            myDevice.Initialize();
            var bList = HueBridge.FindBridges();
            var bData = bList.Select(lb => new BridgeData(lb.IpAddress, lb.BridgeId)).ToList();
            store.InsertItem("myDevice", myDevice);
            store.InsertItem("emuType", "SideKick");
            store.InsertItem("bridges", bData);
            store.InsertItem("devices", Array.Empty<BaseDevice>());
            return store;
        }


        public static dynamic GetItem<T>(string key) {
            try {
                using var dStore = new DataStore(GetConfigPath("store.json"));
                dynamic output = dStore.GetItem<T>(key);
                return output;
            }
            catch (NullReferenceException e) {
                Console.WriteLine($@"Value not found: {e.Message}");
            }
            catch (KeyNotFoundException) { }


            return null;
        }

        public static void SetItem(string key, dynamic value) {
            using var dStore = new DataStore(GetConfigPath("store.json"));
            dStore.ReplaceItem(key, value, true);
        }

        public static void SetItem<T>(string key, dynamic value) {
            using var dStore = new DataStore(GetConfigPath("store.json"));
            dStore.ReplaceItem<T>(key, value, true);
        }

        public static string GetStoreSerialized() {
            var jsonPath = GetConfigPath("store.json");
            if (!File.Exists(jsonPath)) return null;
            try {
                return File.ReadAllText(jsonPath);
            }
            catch (IOException e) {
                Console.WriteLine($@"An IO Exception occurred: {e.Message}.");
            }

            return null;
        }

        public static BaseDevice GetDeviceData() {
            using var dd = GetStore();
            BaseDevice dev;
            string devType = dd.GetItem("emuType");
            if (devType == "SideKick")
                dev = dd.GetItem<SideKick>("myDevice");
            else
                dev = dd.GetItem<Connect>("myDevice");
            return dev;
        }

        /// <summary>
        ///     Determine if config path is local, or docker
        /// </summary>
        /// <param name="filePath">Config file to check</param>
        /// <returns>Modified path to config file</returns>
        private static string GetConfigPath(string filePath) {
            // If no etc dir, return normal path
            if (!Directory.Exists("/etc/huedream")) return filePath;
            // Make our etc path for docker
            var newPath = "/etc/huedream/" + filePath;
            // If the config file doesn't exist locally, we're done
            if (!File.Exists(filePath)) return newPath;
            // Otherwise, move the config to etc
            Console.WriteLine($@"Moving file from {filePath} to {newPath}");
            File.Copy(filePath, newPath);
            File.Delete(filePath);
            return newPath;
        }

        private static string GetLocalIpAddress() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            throw new Exception("No network adapters found in " + JsonConvert.SerializeObject(host));
        }
    }
}