// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Threading.Tasks;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Jobs;
// using UnityEngine;
// using UnityEngine.UI;
// using Debug = UnityEngine.Debug;
//
// /*
//  * 采集摄像头数据，上传rtmp流
//  * 需布置对应的rtmp服务器
//  * 使用flv 播放器可以拉取视频流
//  */
// public class RtmpStream : MonoBehaviour
// {
//     /// <summary>
//     /// 抓取摄像头图像的纹理
//     /// </summary>
//     private Texture2D outputTexture;
//
//     private int frameNum = 0;
//
//     /// <summary>
//     /// 推流进程
//     /// </summary>
//     private Process _proc;
//
//     private Process _pullProcess;
//
//     /// <summary>
//     /// 记录是否正在推流
//     /// </summary>
//     private bool _isRunning = false;
//
//     Camera _camera;
//
//     /// <summary>
//     /// 推流地址
//     /// </summary>
//     string formattedPath = "rtmp://127.0.0.1:1935/live/";
//
//     /// <summary>
//     /// 最大码率 单位kb
//     /// </summary>
//     int maxBitRate = 5000;
//
//     /// <summary>
//     /// 推流码,加在formattedPath后面，区分不同流地址
//     /// </summary>
//     [SerializeField] private string streamCode = "default";
//
//     private Texture2D inputTexture;
//     [SerializeField] private RawImage image;
//     byte[] buffer = new byte[4096];
//
//     private void Awake()
//     {
//     }
//
//     // Start is called before the first frame update
//     void Start()
//     {
//         _camera = GetComponent<Camera>();
//         outputTexture = new Texture2D(_camera.targetTexture.width, _camera.targetTexture.height);
//     }
//
//     // Update is called once per frame
//     void Update()
//     {
//         frameNum++;
//         if (_isRunning && _camera != null && _camera.isActiveAndEnabled)
//         {
//             //UnityEngine.Profiling.Profiler.BeginSample("EncodeNativeArrayToJPG");
//             Stopwatch sw = Stopwatch.StartNew();
//             RenderTexture.active = _camera.targetTexture;
//             outputTexture.ReadPixels(new Rect(0, 0, _camera.targetTexture.width, _camera.targetTexture.height), 0, 0,
//                 true);
//             sw.Stop();
//             Debug.Log($"ReadPixels : {sw.ElapsedMilliseconds} ms");
//             sw.Restart();
//             //UnityEngine.Profiling.Profiler.EndSample();
//             //UnityEngine.Profiling.Profiler.BeginSample("GetRawTextureData");
//             var na = outputTexture.GetRawTextureData<byte>();
//             sw.Stop();
//             Debug.Log($"GetRawTextureData : {sw.ElapsedMilliseconds} ms");
//             sw.Restart();
//             //var val = ImageConversion.EncodeNativeArrayToPNG<byte>(na, outputTexture.graphicsFormat, (uint)outputTexture.width, (uint)outputTexture.height);
//             var val = ImageConversion.EncodeNativeArrayToJPG<byte>(na, outputTexture.graphicsFormat,
//                 (uint)outputTexture.width, (uint)outputTexture.height);
//             sw.Stop();
//             Debug.Log($"EncodeNativeArrayToJPG : {sw.ElapsedMilliseconds} ms");
//             sw.Restart();
//             var val1 = ImageConversion.EncodeNativeArrayToPNG<byte>(na, outputTexture.graphicsFormat,
//                 (uint)outputTexture.width, (uint)outputTexture.height);
//             sw.Stop();
//             Debug.Log($"EncodeNativeArrayToPNG : {sw.ElapsedMilliseconds} ms");
//             sw.Restart();
//             var val2 = ImageConversion.EncodeNativeArrayToTGA<byte>(na, outputTexture.graphicsFormat,
//                 (uint)outputTexture.width, (uint)outputTexture.height);
//             sw.Stop();
//             Debug.Log($"EncodeNativeArrayToTGA : {sw.ElapsedMilliseconds} ms");
//             //UnityEngine.Profiling.Profiler.EndSample();
//             //UnityEngine.Profiling.Profiler.BeginSample("SendImage2Pipe");
//             SendImage2Pipe(val);
//             na.Dispose();
//             val.Dispose();
//             //UnityEngine.Profiling.Profiler.EndSample();
//         }
//     }
//
//     private void OnDisable()
//     {
//         StopCapture();
//     }
//
//     private void OnDestroy()
//     {
//         StopCapture();
//     }
//
//     /// <summary>
//     /// 使用ffmpeg将摄像头图像编码成视频流并推流
//     /// </summary>
//     private void FfmpegToStream()
//     {
//         ReadConfig();
//         _proc = new Process();
//
//         _proc.StartInfo.RedirectStandardInput = true;
//         _proc.StartInfo.RedirectStandardOutput = true;
//         _proc.StartInfo.UseShellExecute = false;
//         _proc.StartInfo.CreateNoWindow = true;
//         _proc.StartInfo.FileName = Application.streamingAssetsPath + "/ffmpeg.exe";
//         _proc.StartInfo.Arguments =
//             $"-f image2pipe -use_wallclock_as_timestamps 1  -i - -c:v libx264 -bf 0 -preset ultrafast -tune zerolatency -r 60 -pix_fmt yuv420p -g 20 -s 512X512 -tcp_nodelay 1 -rtmp_flush_interval 1 -maxrate {maxBitRate}k -f flv {GetRtmpServerUrl()}";
//         _proc.Start();
//     }
//
//     private string GetRtmpServerUrl()
//     {
//         return formattedPath + streamCode;
//     }
//
//     private void CreateProcessGetSteamFromRtmp()
//     {
//         ReadConfig();
//         _pullProcess = new Process();
//
//         _pullProcess.StartInfo.RedirectStandardInput = true;
//         _pullProcess.StartInfo.RedirectStandardOutput = true;
//         _pullProcess.StartInfo.UseShellExecute = false;
//         _pullProcess.StartInfo.CreateNoWindow = true;
//         _pullProcess.StartInfo.FileName = Application.streamingAssetsPath + "/ffmpeg.exe";
//         _pullProcess.StartInfo.Arguments = $"-i {GetRtmpServerUrl()} -f image2pipe -pix_fmt rgb24 -vcodec rawvideo -";
//         _pullProcess.Start();
//     }
//
//     /// <summary>
//     /// 读取配置文件
//     /// </summary>
//     private void ReadConfig()
//     {
//         formattedPath = "rtmp://localhost:1935/live/";
//         maxBitRate = 120;
//     }
//
//     /// <summary>
//     /// 使用ffmpeg将摄像头图像编码成视频流并保存到本地
//     /// </summary>
//     private void FfmpegToVedio()
//     {
//         string formattedPath = "D:\\rtmp.mp4";
//         int maxBitRate = 5000;
//
//         _proc = new Process();
//         _proc.StartInfo.RedirectStandardInput = true;
//         _proc.StartInfo.RedirectStandardOutput = true;
//         _proc.StartInfo.UseShellExecute = false;
//         _proc.StartInfo.CreateNoWindow = true;
//         _proc.StartInfo.FileName = @"ffmpeg";
//         _proc.StartInfo.Arguments =
//             $"-f image2pipe -use_wallclock_as_timestamps 1 -i - -c:v libx264 -vsync passthrough -s 1920x1080 -maxrate {maxBitRate}k -an -y {formattedPath}";
//         _proc.Start();
//     }
//
//     /// <summary>
//     /// 开始推流
//     /// 默认推流地址 rtmp://192.168.48.4:1935/live/
//     /// 默认推流码 default
//     /// 拉流地址为推流地址+推流码即 rtmp://192.168.48.4:1935/live/default
//     /// 推流地址在streamingAssets中的config.ini文件设置，推流码由streamCode变量设置
//     /// 使用前先设置streamCode再推流
//     /// </summary>
//     public void StartCapture()
//     {
//         if (_isRunning)
//             return;
//         Debug.Log("Start");
//         //FfmpegToVedio();
//         FfmpegToStream();
//         _isRunning = true;
//     }
//
//     /// <summary>
//     /// 停止推流
//     /// </summary>
//     public void StopCapture()
//     {
//         _isRunning = false;
//         if (_proc != null)
//         {
//             try
//             {
//                 _proc.StartInfo.RedirectStandardInput = false;
//                 _proc.StartInfo.RedirectStandardOutput = false;
//                 _proc.StandardInput.Close();
//                 _proc.StandardOutput.Close();
//                 _proc.Close();
//             }
//             catch
//             {
//                 Debug.LogError("stop capture error");
//             }
//         }
//
//         Debug.Log("Stop");
//     }
//
//
//     /// <summary>
//     /// 将图片推入管道供编码器使用
//     /// </summary>
//     /// <param name="bytes"></param>
//     private void SendImage2Pipe(Unity.Collections.NativeArray<byte> bytes)
//     {
//         if (_proc.StartInfo.RedirectStandardInput)
//         {
//             _proc.StandardInput.BaseStream.Write(bytes);
//         }
//     }
//
//
//     /// <summary>
//     /// unity editor调试
//     /// </summary>
//     [ContextMenu("start")]
//     private void TestStartCaputre()
//     {
//         StartCapture();
//     }
//
//     /// <summary>
//     /// unity editor调试
//     /// </summary>
//     [ContextMenu("stop")]
//     private void TestStopCaputre()
//     {
//         StopCapture();
//     }
// }