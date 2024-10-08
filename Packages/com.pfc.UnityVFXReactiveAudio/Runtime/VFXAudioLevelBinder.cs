using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace UnityVFXReactiveAudio.Vfx
{
    [AddComponentMenu("VFX/Property Binders/UnityVFXReactiveAudio/Audio Level Binder")]
    [VFXBinder("UnityVFXReactiveAudio/Audio Level")]
    sealed class VFXAudioLevelBinder : VFXBinderBase
    {
        public string Property
          { get => (string)_property; set => _property = value; }

        [VFXPropertyBinding("System.Single"), SerializeField]
        ExposedProperty _property = "AudioLevel";

        public AudioLevelTracker Target = null;

        public override bool IsValid(VisualEffect component)
          => Target != null && component.HasFloat(_property);

        public override void UpdateBinding(VisualEffect component)
          => component.SetFloat(_property, Target.normalizedLevel);

        public override string ToString()
          => $"Audio Level : '{_property}' -> {Target?.name ?? "(null)"}";
    }
}
