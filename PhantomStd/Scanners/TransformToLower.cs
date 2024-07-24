namespace Phantom.Scanners
{
	/// <summary>
	/// Transformer that outputs a lowercase version of the input
	/// </summary>
	public class TransformToLower : ITransform
	{
		#region ITransform Members

		/// <summary>
		/// Convert irregular cased input to lowercased input
		/// </summary>
		string ITransform.Transform(string s) => s.ToLowerInvariant();

		/// <summary>
		/// Convert irregular cased input to lowercased input
		/// </summary>
		char ITransform.Transform(char c) => char.ToLowerInvariant(c);

		#endregion
	}
}