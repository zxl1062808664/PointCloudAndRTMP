using System;
using System.IO;
using UnityEngine;

namespace ZC
{
    internal class RTMPConfig: MonoBehaviour
    {
        public string server = "rtmp://127.0.0.1:1935/live/";
        public string appName = "test1";
        //分辨率
        public string resolution = "1920:1080";
        public string ffmpegPath => $"{Application.streamingAssetsPath}/ffmpeg.exe";
        public string processName = "RTMP";
        public string logLevel = "error";

        public void Load()
        {
            processName = Application.productName;
            string configPath = $"{Application.streamingAssetsPath}/rtmp.config";
            if(!File.Exists(configPath))
                return;
            var readAllLines = File.ReadAllLines(configPath);
            foreach (var line in readAllLines)
            {
                if(line.StartsWith("#"))
                    continue;
                if (line.StartsWith("server"))
                {
                    if (ParseLine(line,out var value)) continue;
                    server = value;
                    continue;
                }

                if (line.StartsWith("streamName"))
                {
                    if (ParseLine(line,out var value)) continue;
                    appName = value;
                    continue;
                }    
                if (line.StartsWith("resolution"))
                {
                    if (ParseLine(line,out var value)) continue;
                    resolution = value;
                    continue;
                }        
                if (line.StartsWith("logLevel"))
                {
                    if (ParseLine(line,out var value)) continue;
                    logLevel = value;
                    continue;
                }
            }
        }

        private static bool ParseLine(string line,out string value)
        {
            value = null;
            int index =line.IndexOf(":", StringComparison.Ordinal);
            if(index==-1)
            {
                Debug.LogError($"Config has error. line:{line}");
                return true;
            }

            value = line.Substring(index + 1);
            return false;
        }
    }
}