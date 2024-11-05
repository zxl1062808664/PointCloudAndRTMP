using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ZC
{
    public class RTMPPublisher : MonoBehaviour
    {
        Thread _thread;
        Process _process;
        private RectInt _rectInt;
        private RTMPConfig _config;
        private const int _rectIntWidth = 1920;
        private const int _rectIntHeight = 1080;


        [SerializeField] private Camera _camera;

        private Texture2D _output;

        private volatile int _sendData;

        private NativeArray<byte> _textureData;
        private AsyncGPUReadbackRequest _asyncGPUReadbackRequest;
        private bool _isRequesting;


        private byte[] _bitmapArray;

        public void SetCamera(Camera camera)
        {
            this._camera = camera;
        }

        // Start is called before the first frame update
        void Start()
        {
            _config = Object.FindAnyObjectByType<RTMPConfig>();
            if (_config == null)
            {
                Debug.LogError("This need the config, plz create a GameObject with RTMPConfig");
                return;
            }

            _config.Load();
            // Setup(Camera.main);
        }

        private void CreateCaptureThread()
        {
            this._thread = new Thread(this.CreateThread);
            this._thread.Start();
        }


        private void OnDestroy()
        {
            Dispose();
        }

        private void Update()
        {
            if (_sendData != 0)
            {
                if (_camera)
                {
                    if (!_isRequesting || _asyncGPUReadbackRequest.done)
                    {
                        var cameraActiveTexture = _camera.targetTexture;
                        RenderTexture.active = cameraActiveTexture;
                        _asyncGPUReadbackRequest = AsyncGPUReadback.RequestIntoNativeArray(ref _textureData, cameraActiveTexture, 0,
                            GraphicsFormat.B8G8R8_SRGB, ReadTextureCallback);
                        _isRequesting = true;
                    }
                }
            }
        }

        private void ReadTextureCallback(AsyncGPUReadbackRequest obj)
        {
            if (obj is { done: true, hasError: false } && this._isRequesting)
            {
                Profiler.BeginSample("Conversion");
                Bitmap.EncodeToBitmap(_textureData, _bitmapArray, 0, _textureData.Length, _output.width, _output.height);
//                var encodeNativeArrayToJPG = ImageConversion.EncodeNativeArrayToJPG(this._textureData, GraphicsFormat.R8G8B8_UNorm, (uint)this._output.width, (uint)this._output.height);
                Profiler.EndSample();
                Profiler.BeginSample("WriteBuffer");
                _process.StandardInput.BaseStream.Write(_bitmapArray, 0, _bitmapArray.Length);
                Profiler.EndSample();
//                encodeNativeArrayToJPG.Dispose();/
            }
        }

        private void DisposeCaptureThread()
        {
            if (_process != null && !this._process.HasExited)
            {
                GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, 0);

//                _process.StandardInput.Close();
                this._process.Kill();
                this._process.Close();
            }

            this._thread?.Abort();
        }

        void CreateThread(object state)
        {
            _process = new Process();
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            processStartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
            processStartInfo.FileName = $"\"{_config.ffmpegPath}\"";
            processStartInfo.Arguments =
                $" -probesize 32 -thread_queue_size 5096 -fflags discardcorrupt -flags low_delay -analyzeduration 0 " +
                $" -rtbufsize 100M -f dshow -i audio=\"virtual-audio-capturer\" " +
                $" -f image2pipe -use_wallclock_as_timestamps 1 -i - " +
                $" -loglevel info " +
                $" -map 0:a:0  -map 1:v:0 " +
                $" -c:a aac -b:a 128k " +
                $" -c:v:0 libx264 -g 1 -max_delay 0 -vf scale={this._config.resolution} -preset:v ultrafast -tune:v zerolatency  -crf 10 -pix_fmt yuv420p -strict -2 " +
                $" -f flv {this._config.server}{this._config.appName} -bf 0 ";
            Debug.Log(processStartInfo.Arguments);

            _process.StartInfo = processStartInfo;
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += (s, e) => { Debug.Log(e.Data); };
            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data) && e.Data.ToLower().Contains("error"))
                {
                    Debug.LogError(e.Data);
                }
                else
                {
                    Debug.Log(e.Data);
                }
            };
            _process.Start();
            Interlocked.Exchange(ref _sendData, 1);
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _process.WaitForExit();
            Debug.Log("Process exited!");
        }

        public void Setup(Camera camera)
        {
            _isRequesting = false;
            SetCamera(camera);
            _sendData = 0;
            Object.Destroy(_output);
            var targetTextureByteCount = camera.targetTexture.width * camera.targetTexture.height * 3;
            if (_bitmapArray == null || _bitmapArray.Length <
                Bitmap.FileHeaderSize + Bitmap.ImageHeaderSize + targetTextureByteCount)
            {
                _bitmapArray = new byte[Bitmap.FileHeaderSize + Bitmap.ImageHeaderSize + targetTextureByteCount];
            }

            _output = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
            _textureData.Dispose();
            _textureData = new NativeArray<byte>(targetTextureByteCount, Allocator.Persistent);
            CreateCaptureThread();
        }

        public void Dispose()
        {
            try
            {
                this._isRequesting = false;
                _sendData = 0;
                _camera = null;
                Object.Destroy(_output);
                this.DisposeCaptureThread();
            }
            finally
            {
#if UNITY_EDITOR
#else
                _textureData.Dispose();
#endif
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);

        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1,
            CTRL_CLOSE = 2,
            CTRL_LOGOFF = 5,
            CTRL_SHUTDOWN = 6
        }
    }
}