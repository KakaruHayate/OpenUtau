using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenUtau.Core.Render;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.DiffSinger {
    public static class OpecExtractor {
        private static InferenceSession? _session;
        private static readonly object _sessionLock = new();
        private static readonly object _inferenceLock = new();

        /// <summary>
        /// Package ID for the OPEC model in Dependencies.
        /// </summary>
        public const string PackageId = "r3moe";

        /// <summary>
        /// ONNX model file name inside the package.
        /// </summary>
        public const string ModelFileName = "opec.onnx";

        /// <summary>
        /// Model hop size in samples (at 44100Hz).
        /// </summary>
        public const int HopSize = 512;

        /// <summary>
        /// Model expected sample rate.
        /// </summary>
        public const int SampleRate = 44100;

        /// <summary>
        /// Check if the OPEC model is installed.
        /// </summary>
        public static bool IsInstalled() {
            var modelPath = ResolveModelPath();
            return File.Exists(modelPath);
        }

        /// <summary>
        /// Resolve the full path to the OPEC ONNX model.
        /// Pattern: {DependencyPath}/opec/opec.onnx
        /// </summary>
        public static string ResolveModelPath() {
            return Path.Combine(PathManager.Inst.DependencyPath, PackageId, ModelFileName);
        }

        /// <summary>
        /// Get or create the cached ONNX inference session.
        /// </summary>
        private static InferenceSession GetSession() {
            if (_session == null) {
                lock (_sessionLock) {
                    if (_session == null) {
                        var modelPath = ResolveModelPath();
                        if (!File.Exists(modelPath)) {
                            throw new FileNotFoundException(
                                $"OPEC model not found at {modelPath}. " +
                                "Please install the opec OUDEP package first.");
                        }
                        var modelBytes = File.ReadAllBytes(modelPath);
                        _session = Onnx.getInferenceSession(modelBytes);
                    }
                }
            }
            return _session;
        }

        /// <summary>
        /// Extract OPEC curve from rendered audio samples.
        /// Runs ONNX inference, smooths the output, and converts frames to project ticks.
        /// Returns null if model is not installed, samples are empty, or cancellation requested.
        /// </summary>
        public static RenderRealCurveResult? Extract(
            RenderPhrase phrase,
            float[] samples,
            CancellationToken cancellationToken) {
            if (samples == null || samples.Length == 0) {
                return null;
            }

            if (!IsInstalled()) {
                Log.Debug("OPEC model not installed, skipping extraction.");
                return null;
            }

            if (cancellationToken.IsCancellationRequested) {
                return null;
            }

            InferenceSession session;
            try {
                session = GetSession();
            } catch (Exception ex) {
                Log.Debug(ex, "Failed to load OPEC model.");
                return null;
            }

            if (cancellationToken.IsCancellationRequested) {
                return null;
            }

            // Build input tensor
            // Input: "waveform", shape [1, samples.Length], float32
            try {
                var inputTensor = new DenseTensor<float>(
                    samples, new[] { 1, samples.Length });

                var inputs = new[] {
                    NamedOnnxValue.CreateFromTensor("waveform", inputTensor)
                };

                float[] curve;
                lock (_inferenceLock) {
                    if (cancellationToken.IsCancellationRequested) {
                        return null;
                    }
                    using var outputs = session.Run(inputs);
                    // Output: "curve", shape [1, frames], float32
                    var outputTensor = outputs.First().AsTensor<float>();
                    curve = outputTensor.ToArray();
                }

                if (curve == null || curve.Length == 0) {
                    return null;
                }

                /// The OPEC model's output frames cover the full input audio,
                /// but we don't know the exact hop ratio — the model may have
                /// internal striding.  Instead of assuming a frameMs value that
                /// matches the DiffSinger acoustic model (which is different from
                /// OPEC's internal frame rate), interpolate the curve evenly
                /// across the audio time span.
                ///
                /// The audio covers roughly:
                ///   [phrase.position, phrase.position + audioDuration]
                /// We widen slightly to include DiffSinger's leading/trailing
                /// padding so the real curve overlaps the displayed note range.

                // Smooth BEFORE interpolation to avoid aliasing of the
                // smoothed shape when down- or up-sampling.
                var smoothed = SinusoidalSmoother.Smooth(curve);

                // Convert frames to phrase-relative ticks.
                // Audio duration = samples.Length / SampleRate seconds.
                double audioDurationMs = (double)samples.Length / SampleRate * 1000.0;
                float[] ticks = new float[smoothed.Length];
                for (int i = 0; i < smoothed.Length; i++) {
                    double fraction = (double)i / smoothed.Length;
                    double frameTimeMs = phrase.positionMs + fraction * audioDurationMs;
                    double absTick = phrase.timeAxis.MsPosToTickPos(frameTimeMs);
                    ticks[i] = (float)(absTick - phrase.position);
                }

                return new RenderRealCurveResult {
                    abbr = "shmc",
                    ticks = ticks,
                    values = smoothed,
                };
            } catch (Exception ex) {
                Log.Debug(ex, "OPEC extraction failed.");
                return null;
            }
        }

        /// <summary>
        /// Dispose the cached session. Call on app shutdown.
        /// </summary>
        public static void DisposeSession() {
            lock (_sessionLock) {
                _session?.Dispose();
                _session = null;
            }
        }
    }
}