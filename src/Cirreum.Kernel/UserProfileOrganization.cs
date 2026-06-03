namespace Cirreum;

using System.Collections.Immutable;
using System.Text.Json.Serialization;

/// <summary>
/// Represents organizational information across different identity providers.
/// Normalizes various organizational concepts like tenants, realms, and workspaces.
/// </summary>
public record UserProfileOrganization {

	public static UserProfileOrganization Empty { get; } = new UserProfileOrganization {
		OrganizationId = "None"
	};

	[JsonIgnore]
	public bool IsEmpty => this.OrganizationId is null or "" or "None";

	/// <summary>
	/// Required. Unique identifier for the organization, normalized across providers
	/// </summary>
	public required string OrganizationId { get; init; }

	/// <summary>
	/// Display name of the organization
	/// </summary>
	public string? OrganizationName { get; init; }

	/// <summary>
	/// Organizational roles the user has been assigned.
	/// </summary>
	[JsonIgnore]
	public ImmutableList<UserProfileMembership> DirectoryRoles
		=> [.. (this.DirectoryRolesRaw ?? [])];
	[JsonInclude]
	internal List<UserProfileMembership> DirectoryRolesRaw { get; init; } = [];

	/// <summary>
	/// Organizational groups the user is a member-of.
	/// </summary>
	[JsonIgnore]
	public ImmutableList<UserProfileMembership> DirectoryGroups
		=> [.. (this.DirectoryGroupsRaw ?? [])];
	/// <summary>
	/// Mutable backing list for directory-group membership. Populated by enricher
	/// implementations (e.g., <c>ClaimsUserProfileEnricher</c> in
	/// Cirreum.AuthenticationProvider) during profile construction. Read-only consumers
	/// should use <see cref="DirectoryGroups"/> which returns an immutable snapshot.
	/// </summary>
	[JsonInclude]
	public List<UserProfileMembership> DirectoryGroupsRaw { get; init; } = [];

	/// <summary>
	/// Additional provider-specific metadata
	/// </summary>
	public Dictionary<string, object> Metadata { get; set; } = [];

}