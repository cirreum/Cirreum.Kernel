namespace Cirreum.State;

/// <summary>
/// Marker interface for all application state types.
/// </summary>
/// <remarks>
/// This interface provides a common root type for state objects used by the
/// Cirreum state system. It enables APIs to constrain operations to state
/// instances without requiring a specific state implementation.
/// <para>
/// This interface is typically not implemented directly. Instead, implement
/// one of the derived interfaces such as <c>IScopedNotificationState</c>,
/// <c>IStateContainer</c>, or <c>IPersistableStateContainer</c>
/// which provide state behavior.
/// </para>
/// </remarks>
public interface IApplicationState;