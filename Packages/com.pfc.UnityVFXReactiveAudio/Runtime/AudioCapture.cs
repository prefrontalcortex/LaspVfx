using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace UnityVFXReactiveAudio
{
    // Filter type enums used in audio input processing
    public enum FilterType { Bypass, LowPass, BandPass, HighPass }

    [AddComponentMenu("UnityVFXReactiveAudio/Audio Capture")]
    public sealed class AudioCapture : MonoBehaviour
    {
        #region Stream settings

        public bool IsReady { get; private set; }
        public int ChannelCount { get; private set; }
        
        public int SampleRate { get; private set; }
        
        #endregion

        #region Per-channel audio levels
        
        private LevelMeter _audioLevels;
        
        public float GetChannelLevel(int channel)
        {
            if (!IsReady || _audioLevels == null)
                return 0;
            return MathUtils.dBFS(_audioLevels.GetLevel(channel).x);
        }

        public float GetChannelLevel(int channel, FilterType filter)
        {
            if (!IsReady)
                return 0;
            return MathUtils.dBFS(_audioLevels.GetLevel(channel)[(int)filter]);
        }

        #endregion

        #region Audio data (waveform)
        
        public NativeSlice<float> InterleavedDataSlice
        {
            get
            {
                return new NativeSlice<float>(_readingAudioBuffer.AsArray());
            }
        }

        public NativeSlice<float> GetChannelDataSlice(int channel)
        {
            if (!IsReady || _readingAudioBuffer.Length < channel+1)
                return default;
            
            return new NativeSlice<float>(_readingAudioBuffer.AsArray(), channel).GetNativeSlice(channel, ChannelCount);
        }

        #endregion

        private NativeList<float> _readingAudioBuffer;
        private NativeList<float> _fillUpAudioBuffer;
        
        private object _bufferLock = new object();
        private int _channels;
        
        private void OnEnable()
        {
            SampleRate = AudioSettings.outputSampleRate;
            // TODO: better initial capacity based on channels, buffersize, etc.
            var initialCapacity = 1024 * 4 * 2;
            
            lock (_bufferLock)
            {
                _channels = -1;
                _readingAudioBuffer = new NativeList<float>(initialCapacity,Allocator.Persistent);
                _fillUpAudioBuffer = new NativeList<float>(initialCapacity,Allocator.Persistent);
            }

            IsReady = false;
        }

        private void OnDisable()
        {
            lock (_bufferLock)
            {
                _readingAudioBuffer.Dispose();
                _fillUpAudioBuffer.Dispose();
                _audioLevels = null;
            }

            IsReady = false;
        }
        
        private void LateUpdate()
        {
            lock (_bufferLock)
            {
                ChannelCount = _channels;
                if (!_readingAudioBuffer.IsCreated)
                    return;

                (_readingAudioBuffer, _fillUpAudioBuffer) = (_fillUpAudioBuffer, _readingAudioBuffer);

                _fillUpAudioBuffer.Clear();
            }

            if (ChannelCount > 0)
            {
                IsReady = true;

                if (_audioLevels == null)
                {
                    _audioLevels = new LevelMeter(ChannelCount);
                    _audioLevels.SampleRate = SampleRate;
                }

                _audioLevels.ProcessAudioData(_readingAudioBuffer.AsArray());
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (_bufferLock)
            {
                _channels = channels;
                if (!_fillUpAudioBuffer.IsCreated)
                    return;

                unsafe
                {
                    var dataPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(data, out var dataHandle);
                    var nativeData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(dataPtr, data.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var safety = AtomicSafetyHandle.Create();
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeData, safety);
#endif
                    _fillUpAudioBuffer.AddRange(nativeData);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.Release(safety);
#endif                    
                    UnsafeUtility.ReleaseGCObject(dataHandle);
                    
                    // In case the game is paused, clear the buffer to avoid a growing buffer
                    if (_fillUpAudioBuffer.Length > _channels * 48000 / 4)
                        _fillUpAudioBuffer.Clear();
                }
            }
        }
    }
}
