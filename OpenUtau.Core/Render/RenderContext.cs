using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OpenUtau.Core.Render {
    internal static class RenderContext {
        static readonly AsyncLocal<PreRenderPriority[]> preRenderPriorities = new AsyncLocal<PreRenderPriority[]>();

        internal static PreRenderPriority[] PreRenderPriorities =>
            preRenderPriorities.Value ?? Array.Empty<PreRenderPriority>();

        internal static IDisposable WithPreRenderPriorities(IEnumerable<PreRenderPriority> priorities) {
            var previous = preRenderPriorities.Value;
            preRenderPriorities.Value = priorities.ToArray();
            return new Scope(() => preRenderPriorities.Value = previous);
        }

        class Scope : IDisposable {
            readonly Action dispose;
            bool disposed;

            public Scope(Action dispose) {
                this.dispose = dispose;
            }

            public void Dispose() {
                if (!disposed) {
                    dispose();
                    disposed = true;
                }
            }
        }
    }
}
