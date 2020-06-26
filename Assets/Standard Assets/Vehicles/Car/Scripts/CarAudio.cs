using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarAudio : MonoBehaviour
    {
        // 这个脚本需要读取一些车辆的当前数据，来播放相应的声音
        // 引擎的声音可以是一段简单的循环片段，或者它也可以是能描述引擎转速或者油门的不同的四个变化的混合片段
        // This script reads some of the car's current properties and plays sounds accordingly.
        // The engine sound can be a simple single clip which is looped and pitched, or it
        // can be a crossfaded blend of four clips which represent the timbre of the engine
        // at different RPM and Throttle state.

        // 引擎片段应当平缓而不是正在升调或者降调
        // the engine clips should all be a steady pitch, not rising or falling.

        // 当使用四个通道的片段时
        // 低加速片段：引擎转速低时，油门打开
        // 高加速片段：引擎转速高时，油门打开
        // 低减速片段：引擎转速低时，油门最小
        // 高减速片段：引擎转速高时，油门最小
        // when using four channel engine crossfading, the four clips should be:
        // lowAccelClip : The engine at low revs, with throttle open (i.e. begining acceleration at very low speed)
        // highAccelClip : Thenengine at high revs, with throttle open (i.e. accelerating, but almost at max speed)
        // lowDecelClip : The engine at low revs, with throttle at minimum (i.e. idling or engine-braking at very low speed)
        // highDecelClip : Thenengine at high revs, with throttle at minimum (i.e. engine-braking at very high speed)

        // 为了得到正确的过渡音，片段音调应当符合
        // For proper crossfading, the clips pitches should all match, with an octave offset between low and high.

        // 总之就是使用四个声音片段插值得到平滑的声音，或者直接使用单个的声音文件

        // 可以选择单一声音或者四通道
        public enum EngineAudioOptions // Options for the engine audio
        {
            Simple, // Simple style audio
            FourChannel // four Channel audio
        }

        public EngineAudioOptions engineSoundStyle = EngineAudioOptions.FourChannel;// Set the default audio options to be four channel
        public AudioClip lowAccelClip;                                              // Audio clip for low acceleration
        public AudioClip lowDecelClip;                                              // Audio clip for low deceleration
        public AudioClip highAccelClip;                                             // Audio clip for high acceleration
        public AudioClip highDecelClip;                                             // Audio clip for high deceleration
        public float pitchMultiplier = 1f;                                          // Used for altering the pitch of audio clips
        public float lowPitchMin = 1f;                                              // The lowest possible pitch for the low sounds
        public float lowPitchMax = 6f;                                              // The highest possible pitch for the low sounds
        public float highPitchMultiplier = 0.25f;                                   // Used for altering the pitch of high sounds
        public float maxRolloffDistance = 500;                                      // The maximum distance where rollof starts to take place
        public float dopplerLevel = 1;                                              // The mount of doppler effect used in the audio
        public bool useDoppler = true;                                              // Toggle for using doppler

        private AudioSource m_LowAccel; // Source for the low acceleration sounds
        private AudioSource m_LowDecel; // Source for the low deceleration sounds
        private AudioSource m_HighAccel; // Source for the high acceleration sounds
        private AudioSource m_HighDecel; // Source for the high deceleration sounds
        private bool m_StartedSound; // flag for knowing if we have started sounds
        private CarController m_CarController; // Reference to car we are controlling

        // 开始播放
        private void StartSound()
        {
            // get the carcontroller ( this will not be null as we have require component)
            m_CarController = GetComponent<CarController>();

            // 先设置高加速片段
            // setup the simple audio source
            m_HighAccel = SetUpEngineAudioSource(highAccelClip);

            // 如果使用四通道则设置其他三个片段
            // if we have four channel audio setup the four audio sources
            if (engineSoundStyle == EngineAudioOptions.FourChannel)
            {
                m_LowAccel = SetUpEngineAudioSource(lowAccelClip);
                m_LowDecel = SetUpEngineAudioSource(lowDecelClip);
                m_HighDecel = SetUpEngineAudioSource(highDecelClip);
            }

            // 开始播放的旗帜
            // flag that we have started the sounds playing
            m_StartedSound = true;
        }

        // 停止播放
        private void StopSound()
        {
            // 去除掉所有的音效片段
            //Destroy all audio sources on this object:
            foreach (var source in GetComponents<AudioSource>())
            {
                Destroy(source);
            }

            m_StartedSound = false;
        }


        // Update is called once per frame
        private void Update()
        {
            // 车辆和摄像机的距离
            // get the distance to main camera
            float camDist = (Camera.main.transform.position - transform.position).sqrMagnitude;

            // 距离超过了最大距离，停止播放
            // stop sound if the object is beyond the maximum roll off distance
            if (m_StartedSound && camDist > maxRolloffDistance*maxRolloffDistance)
            {
                StopSound();
            }

            // 小于最大距离，开始播放
            // start the sound if not playing and it is nearer than the maximum distance
            if (!m_StartedSound && camDist < maxRolloffDistance*maxRolloffDistance)
            {
                StartSound();
            }

            if (m_StartedSound)
            {
                // 根据引擎转速的插值
                // The pitch is interpolated between the min and max values, according to the car's revs.
                float pitch = ULerp(lowPitchMin, lowPitchMax, m_CarController.Revs);

                // clamp一下，那为什么上一句不用Lerp？
                // clamp to minimum pitch (note, not clamped to max for high revs while burning out)
                pitch = Mathf.Min(lowPitchMax, pitch);

                if (engineSoundStyle == EngineAudioOptions.Simple)
                {
                    // 单通道，简单设置音调，多普勒等级，音量
                    // for 1 channel engine sound, it's oh so simple:
                    m_HighAccel.pitch = pitch*pitchMultiplier*highPitchMultiplier;
                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighAccel.volume = 1;
                }
                else
                {
                    // for 4 channel engine sound, it's a little more complex:

                    // 根据pitch和音调乘数调整音调
                    // adjust the pitches based on the multipliers
                    m_LowAccel.pitch = pitch*pitchMultiplier;
                    m_LowDecel.pitch = pitch*pitchMultiplier;
                    m_HighAccel.pitch = pitch*highPitchMultiplier*pitchMultiplier;
                    m_HighDecel.pitch = pitch*highPitchMultiplier*pitchMultiplier;

                    // get values for fading the sounds based on the acceleration
                    float accFade = Mathf.Abs(m_CarController.AccelInput);
                    float decFade = 1 - accFade;

                    // get the high fade value based on the cars revs
                    float highFade = Mathf.InverseLerp(0.2f, 0.8f, m_CarController.Revs);
                    float lowFade = 1 - highFade;

                    // adjust the values to be more realistic
                    highFade = 1 - ((1 - highFade)*(1 - highFade));
                    lowFade = 1 - ((1 - lowFade)*(1 - lowFade));
                    accFade = 1 - ((1 - accFade)*(1 - accFade));
                    decFade = 1 - ((1 - decFade)*(1 - decFade));

                    // adjust the source volumes based on the fade values
                    m_LowAccel.volume = lowFade*accFade;
                    m_LowDecel.volume = lowFade*decFade;
                    m_HighAccel.volume = highFade*accFade;
                    m_HighDecel.volume = highFade*decFade;

                    // adjust the doppler levels
                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                }
            }
        }

        // 添加一个音效片段
        // sets up and adds new audio source to the gane object
        private AudioSource SetUpEngineAudioSource(AudioClip clip)
        {
            // create the new audio source component on the game object and set up its properties
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = 0;
            source.loop = true;

            // 在音效片段的随机位置开始播放
            // start the clip from a random point
            source.time = Random.Range(0f, clip.length);
            source.Play();
            source.minDistance = 5;
            source.maxDistance = maxRolloffDistance;
            source.dopplerLevel = 0;
            return source;
        }


        // unclamped versions of Lerp and Inverse Lerp, to allow value to exceed the from-to range
        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value)*from + value*to;
        }
    }
}
