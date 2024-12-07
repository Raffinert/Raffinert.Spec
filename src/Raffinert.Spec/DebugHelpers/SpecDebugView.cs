using System.Diagnostics;
using System.Linq.Expressions;

namespace Raffinert.Spec.DebugHelpers;

internal class SpecDebugView(ISpec spec)
{
    public DebugViewInternal DebugView { get; } = new DebugViewInternal(spec);

    internal class DebugViewInternal
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ISpec _spec;

        public DebugViewInternal(ISpec spec)
        {
            _spec = spec;
        }

        public LambdaExpression Expression => _spec.GetExpression();
        public LambdaExpression ExpandedExpression => _spec.GetExpandedExpression();
    }
}
