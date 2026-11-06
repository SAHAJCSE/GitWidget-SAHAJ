using System;
using System.IO;
using System.Media;

namespace GitHubDesktopWidget.Services
{
    public static class SoundService
    {
        public static bool IsMuted { get; set; } = false;

        private static byte[]? _popWav;
        private static byte[]? _chimeWav;
        private static byte[]? _tickWav;
        private static byte[]? _angryWav;
        private static byte[]? _sleepyWav;

        static SoundService()
        {
            try
            {
                _popWav = GeneratePopWave();
                _chimeWav = GenerateChimeWave();
                _tickWav = GenerateTickWave();
                _angryWav = GenerateAngryWave();
                _sleepyWav = GenerateSleepyWave();
            }
            catch { }
        }

        public static void PlayPop() => PlaySafe(_popWav);
        public static void PlayChime() => PlaySafe(_chimeWav);
        public static void PlayTick() => PlaySafe(_tickWav);
        public static void PlayAngry() => PlaySafe(_angryWav);
        public static void PlaySleepy() => PlaySafe(_sleepyWav);

        private static void PlaySafe(byte[]? wavData)
        {
            if (IsMuted || wavData == null) return;
            try
            {
                using var ms = new MemoryStream(wavData);
                using var player = new SoundPlayer(ms);
                player.Play();
            }
            catch { }
        }

        // 1. Subtle Bubble Pop (Frequency drop 520Hz -> 200Hz, 40ms)
        private static byte[] GeneratePopWave()
        {
            const int sampleRate = 44100;
            const double duration = 0.045;
            int numSamples = (int)(sampleRate * duration);
            var samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double freq = 540.0 - (t / duration) * 320.0;
                double env = Math.Exp(-t * 80.0);
                samples[i] = (short)(Math.Sin(2 * Math.PI * freq * t) * env * 14000);
            }

            return BuildWavFile(samples, sampleRate);
        }

        // 2. Cheerful Double Chime (C6 -> E6 chord, 140ms)
        private static byte[] GenerateChimeWave()
        {
            const int sampleRate = 44100;
            const double duration = 0.16;
            int numSamples = (int)(sampleRate * duration);
            var samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double tone1 = Math.Sin(2 * Math.PI * 1046.5 * t) * Math.Exp(-t * 22.0); // C6
                double tone2 = t > 0.05 ? Math.Sin(2 * Math.PI * 1318.5 * (t - 0.05)) * Math.Exp(-(t - 0.05) * 20.0) : 0; // E6
                samples[i] = (short)((tone1 * 0.6 + tone2 * 0.7) * 16000);
            }

            return BuildWavFile(samples, sampleRate);
        }

        // 3. Tactile Switch Click (8ms subtle wooden transient)
        private static byte[] GenerateTickWave()
        {
            const int sampleRate = 44100;
            const double duration = 0.015;
            int numSamples = (int)(sampleRate * duration);
            var samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 250.0);
                samples[i] = (short)(Math.Sin(2 * Math.PI * 880.0 * t) * env * 12000);
            }

            return BuildWavFile(samples, sampleRate);
        }

        // 4. Low Angry Grumble (Low square-ish vibrato, 120ms)
        private static byte[] GenerateAngryWave()
        {
            const int sampleRate = 44100;
            const double duration = 0.12;
            int numSamples = (int)(sampleRate * duration);
            var samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double freq = 140.0 + Math.Sin(2 * Math.PI * 25 * t) * 30.0;
                double env = Math.Exp(-t * 18.0);
                samples[i] = (short)(Math.Sign(Math.Sin(2 * Math.PI * freq * t)) * env * 9000);
            }

            return BuildWavFile(samples, sampleRate);
        }

        // 5. Gentle Sleepy Descending Tone (320Hz -> 180Hz, 180ms)
        private static byte[] GenerateSleepyWave()
        {
            const int sampleRate = 44100;
            const double duration = 0.18;
            int numSamples = (int)(sampleRate * duration);
            var samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double freq = 320.0 - (t / duration) * 140.0;
                double env = Math.Sin(Math.PI * (t / duration)) * Math.Exp(-t * 6.0);
                samples[i] = (short)(Math.Sin(2 * Math.PI * freq * t) * env * 12000);
            }

            return BuildWavFile(samples, sampleRate);
        }

        private static byte[] BuildWavFile(short[] samples, int sampleRate)
        {
            int subChunk2Size = samples.Length * 2;
            using var ms = new MemoryStream(44 + subChunk2Size);
            using var bw = new BinaryWriter(ms);

            // RIFF header
            bw.Write(new char[] { 'R', 'I', 'F', 'F' });
            bw.Write(36 + subChunk2Size);
            bw.Write(new char[] { 'W', 'A', 'V', 'E' });

            // fmt subchunk
            bw.Write(new char[] { 'f', 'm', 't', ' ' });
            bw.Write(16); // SubChunk1Size (PCM)
            bw.Write((short)1); // AudioFormat (PCM)
            bw.Write((short)1); // NumChannels (Mono)
            bw.Write(sampleRate);
            bw.Write(sampleRate * 2); // ByteRate
            bw.Write((short)2); // BlockAlign
            bw.Write((short)16); // BitsPerSample

            // data subchunk
            bw.Write(new char[] { 'd', 'a', 't', 'a' });
            bw.Write(subChunk2Size);
            foreach (var s in samples)
            {
                bw.Write(s);
            }

            return ms.ToArray();
        }
    }
}
