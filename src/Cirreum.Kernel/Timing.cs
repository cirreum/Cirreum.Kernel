namespace Cirreum;

using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// High-performance timestamp helpers built on <see cref="Stopwatch"/>. Used by
/// higher-layer types (e.g., <c>OperationContext</c> in Cirreum.Contracts) to measure
/// elapsed time without allocating <see cref="Stopwatch"/> instances per operation.
/// </summary>
public static class Timing {

	/// <summary>Returns the current high-resolution timestamp.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long Start() => Stopwatch.GetTimestamp();

	/// <summary>Returns the elapsed <see cref="TimeSpan"/> since the given start timestamp.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TimeSpan GetElapsedTime(long startTimestamp)
		=> Stopwatch.GetElapsedTime(startTimestamp);

	/// <summary>Returns the elapsed milliseconds since the given start timestamp.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double GetElapsedMilliseconds(long startTimestamp)
		=> Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

}
