﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HueDream.Models.Util;
using Newtonsoft.Json;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi.Models.Groups;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;

namespace HueDream.Models.Hue {
    public class HueBridge : IDisposable {
        private readonly BridgeData bd;
        private EntertainmentLayer entLayer;
        private StreamingHueClient client;
        private bool disposed;
        private bool streaming;

        public HueBridge(BridgeData data) {
            bd = data ?? throw new ArgumentNullException(nameof(data));
            BridgeIp = bd.Ip;
            BridgeKey = bd.Key;
            BridgeUser = bd.User;
            client = StreamingSetup.GetClient(bd);
            disposed = false;
            streaming = false;
            entLayer = null;
            Console.WriteLine(@"Hue: Loading bridge: " + BridgeIp);
        }

        private string BridgeIp { get; }
        private string BridgeKey { get; }
        private string BridgeUser { get; }


        /// <summary>
        ///     Set up and create a new streaming layer based on our light map
        /// </summary>
        /// <param name="ct">A cancellation token.</param>
        public void EnableStreaming(CancellationToken ct) {
            // Get our light map and filter for mapped lights
            Console.WriteLine($@"Hue: Connecting to bridge at {BridgeIp}...");
            // Grab our stream
            var stream = StreamingSetup.SetupAndReturnGroup(client, bd, ct).Result;
            // This is what we actually need
            entLayer = stream.GetNewLayer(true);
            LogUtil.WriteInc($"Streaming established: {BridgeIp}");
            streaming = true;

        }
        
        public void DisableStreaming() {
            var unused = StreamingSetup.StopStream(client, bd);
            LogUtil.WriteDec($"Streaming stopped: {BridgeIp}");
            streaming = false;
        }

        /// <summary>
        ///     Update lights in entertainment layer
        /// </summary>
        /// <param name="colors">An array of 12 colors corresponding to sector data</param>
        /// <param name="brightness">The general brightness of the device</param>
        /// <param name="ct">A cancellation token</param>
        /// <param name="fadeTime">Optional: how long to fade to next state</param>
        public void UpdateLights(string[] colors, int brightness, CancellationToken ct, double fadeTime = 0) {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            if (entLayer != null) {
                var lightMappings = bd.Lights;
                // Loop through lights in entertainment layer
                //Console.WriteLine(@"Sending to bridge...");
                foreach (var entLight in entLayer) {
                    // Get data for our light from map
                    var lightData = lightMappings.SingleOrDefault(item => item.Id == entLight.Id.ToString());
                    // Return if not mapped
                    if (lightData == null) continue;
                    // Otherwise, get the corresponding sector color
                    var colorString = colors[lightData.TargetSector];
                    // Make it into a color
                    var endColor = ClampBrightness(colorString, lightData, brightness);
                    //var xyColor = HueColorConverter.RgbToXY(endColor, CIE1931Gamut.PhilipsWideGamut);
                    //endColor = HueColorConverter.XYToRgb(xyColor, GetLightGamut(lightData.ModelId));
                    // If we're currently using a scene, animate it
                    if (fadeTime != 0) // Our start color is the last color we had
                        entLight.SetState(ct, endColor, endColor.GetBrightness(),
                            TimeSpan.FromSeconds(fadeTime));
                    else // Otherwise, if we're streaming, just set the color
                        entLight.SetState(ct, endColor, endColor.GetBrightness());
                    //entLight.State.SetRGBColor(endColor);
                    //entLight.State.SetBrightness(endColor.GetBrightness());
                }
            }
            else {
                Console.WriteLine($@"Hue: Unable to fetch entertainment layer. {BridgeIp}");
            }
        }

        private RGBColor ClampBrightness(string colorString, LightData lightData, int brightness) {
            var oColor = new RGBColor(colorString);
            // Clamp our brightness based on settings
            long bClamp = 255 * brightness / 100;
            if (lightData.OverrideBrightness) {
                var newB = lightData.Brightness;
                bClamp = 255 * newB / 100;
            }

            var hsb = new HSB((int) oColor.GetHue(), (int) oColor.GetSaturation(), (int) oColor.GetBrightness());
            if (hsb.Brightness > bClamp) hsb.Brightness = (int) bClamp;
            oColor = hsb.GetRGB();

            return oColor;
        }


        public async Task<List<Group>> ListGroups() {
            var all = await client.LocalHueClient.GetEntertainmentGroups().ConfigureAwait(true);
            var output = new List<Group>();
            output.AddRange(all);
            return output;
        }


        public static async Task<RegisterEntertainmentResult> CheckAuth(string bridgeIp) {
            try {
                ILocalHueClient client = new LocalHueClient(bridgeIp);
                //Make sure the user has pressed the button on the bridge before calling RegisterAsync
                //It will throw an LinkButtonNotPressedException if the user did not press the button
                var result = await client.RegisterAsync("HueDream", Environment.MachineName, true);
                Console.WriteLine($@"Hue: User name is {result.Username}.");
                return result;
            }
            catch (HueException) {
                Console.WriteLine($@"Hue: The link button is not pressed at {bridgeIp}.");
            }

            return null;
        }


        public static LocatedBridge[] FindBridges(int time = 2) {
            Console.WriteLine(@"Hue: Looking for bridges...");
            IBridgeLocator locator = new SsdpBridgeLocator();
            var res = locator.LocateBridgesAsync(TimeSpan.FromSeconds(time)).Result;
            Console.WriteLine($@"Result: {JsonConvert.SerializeObject(res)}");
            return res.ToArray();
        }

        public List<LightData> GetLights() {
            // If we have no IP or we're not authorized, return
            if (BridgeIp == "0.0.0.0" || BridgeUser == null || BridgeKey == null) return new List<LightData>();
            // Create client
            Console.WriteLine(@"Hue: Enumerating lights.");
            client.LocalHueClient.Initialize(BridgeUser);
            // Get lights
            var lights = bd.Lights ?? new List<LightData>();
            var res = client.LocalHueClient.GetLightsAsync().Result;
            var ld = res.Select(r => new LightData(r)).ToList();
            var output = new List<LightData>();
            foreach (var light in ld) {
                var add = true;
                foreach (var unused in lights.Where(oLight => oLight.Id == light.Id)) add = false;
                if (add) output.Add(light);
            }

            lights.AddRange(output);
            return lights;
        }
       
        
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        public void Dispose(bool disposing) {
            if (disposed) {
                return;
            }

            if (disposing) {
                if (streaming) {
                    DisableStreaming();
                }
                client?.Dispose();
            }

            disposed = true;
        }
        
    }
}