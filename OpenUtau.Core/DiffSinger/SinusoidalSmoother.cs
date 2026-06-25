using System;
using System.Linq;

namespace OpenUtau.Core.DiffSinger {
    public static class SinusoidalSmoother {
        /// <summary>
        /// Applies sinusoidal (half-sine) smoothing convolution to a 1D curve.
        /// This replicates SinusoidalSmoothingConv1d from the DiffSinger SHMC2 preprocessing pipeline.
        /// kernel_size = round(width / timestep), where width=0.06 and timestep=512/44100 ≈ 5.
        /// Uses 'same' output size with replicate padding (edge values clamped).
        /// </summary>
        public static float[] Smooth(float[] curve, int kernelSize = 5) {
            if (curve == null || curve.Length == 0 || kernelSize <= 1) {
                return curve ?? Array.Empty<float>();
            }

            // Build half-sine kernel
            // sin(linspace(0, 1, kernelSize) * pi)
            float[] kernel = new float[kernelSize];
            for (int i = 0; i < kernelSize; i++) {
                kernel[i] = (float)Math.Sin((double)i / (kernelSize - 1) * Math.PI);
            }

            // Normalize kernel to sum = 1
            float sum = kernel.Sum();
            if (sum > 0) {
                for (int i = 0; i < kernelSize; i++) {
                    kernel[i] /= sum;
                }
            }

            // Convolve with replicate padding (clamp indices to [0, length-1])
            int n = curve.Length;
            int halfK = kernelSize / 2;
            float[] result = new float[n];

            for (int i = 0; i < n; i++) {
                double accum = 0;
                for (int j = 0; j < kernelSize; j++) {
                    int srcIdx = i + j - halfK;
                    srcIdx = Math.Clamp(srcIdx, 0, n - 1);
                    accum += curve[srcIdx] * kernel[j];
                }
                result[i] = (float)accum;
            }

            return result;
        }
    }
}