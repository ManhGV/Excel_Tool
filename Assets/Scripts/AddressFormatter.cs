using System.Text.RegularExpressions;

/// <summary>
/// Utility class for formatting Vietnamese addresses
/// Cleans up address strings by removing newlines, extra spaces, and organizing hierarchical structure
/// </summary>
public static class AddressFormatter
{
    /// <summary>
    /// Format a raw address string by cleaning and reorganizing components
    /// Example: "239:Bình Phước\\nThôn Phú Hưng xã Phú Riềng huyện Phú Riềng tỉnh Bình Phước \\n"
    /// becomes: "Thôn Phú Hưng, Xã Phú Riềng, Huyện Phú Riềng, Tỉnh Bình Phước"
    /// </summary>
    public static string FormatAddress(string rawAddress)
    {
        if (string.IsNullOrWhiteSpace(rawAddress))
            return "";

        // Step 1: Remove ALL separators and special characters to start fresh
        // This ensures multiple formatting doesn't add more commas
        string cleaned = RemoveAllSeparators(rawAddress);
        
        // Step 2: Remove numbering prefix (e.g., "239:")
        cleaned = Regex.Replace(cleaned, @"^\d+:\s*", "");
        cleaned = cleaned.Trim();

        // Step 3: Remove colon and Vietnam suffix if exists
        cleaned = Regex.Replace(cleaned, @"\s*:\s*Vietnam\s*$", "", RegexOptions.IgnoreCase);
        cleaned = cleaned.Trim();

        // Step 4: Extract and organize address components
        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = NormalizeAddressComponents(cleaned);
        }

        return cleaned;
    }

    /// <summary>
    /// Remove ALL types of separators and special characters from the address string
    /// This prevents multiple formatting from accumulating commas
    /// </summary>
    private static string RemoveAllSeparators(string address)
    {
        // Replace all types of separators with space
        string cleaned = address;
        
        // Remove newlines (both \n and \\n)
        cleaned = cleaned.Replace("\\n", " ").Replace("\n", " ");
        
        // Remove backslashes (both \\ and single \)
        cleaned = cleaned.Replace("\\", " ").Replace("\\", " ");
        
        // Remove forward slashes
        cleaned = cleaned.Replace("/", " ");
        
        // Remove ALL existing commas and extra spaces around them
        cleaned = cleaned.Replace(",", " ");
        
        // Collapse multiple spaces into single space
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        
        // Trim
        cleaned = cleaned.Trim();
        
        return cleaned;
    }

    /// <summary>
    /// Normalize and organize Vietnamese address components
    /// Recognizes patterns like: thôn/ấp, xã/phường, huyện/quận, tỉnh/thành phố
    /// </summary>
    private static string NormalizeAddressComponents(string address)
    {
        // Dictionary of address component patterns
        string thonAp = ExtractComponent(address, new[] { "thôn", "ấp" });
        string xaPhuong = ExtractComponent(address, new[] { "xã", "phường", "phư" });
        string huyenQuan = ExtractComponent(address, new[] { "huyện", "quận" });
        string tinhThanh = ExtractComponent(address, new[] { "tỉnh", "thành phố", "thành" });

        // Build formatted address
        var parts = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrEmpty(thonAp))
            parts.Add(thonAp);
        if (!string.IsNullOrEmpty(xaPhuong))
            parts.Add(xaPhuong);
        if (!string.IsNullOrEmpty(huyenQuan))
            parts.Add(huyenQuan);
        if (!string.IsNullOrEmpty(tinhThanh))
            parts.Add(tinhThanh);

        // If no components found, return original (might be a simple address)
        if (parts.Count == 0)
            return address;

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Extract a single address component and its value
    /// E.g., extracting "xã Phú Riềng" returns "Xã Phú Riềng"
    /// </summary>
    private static string ExtractComponent(string address, string[] patterns)
    {
        string lowerAddress = address.ToLower();

        foreach (string pattern in patterns)
        {
            int index = lowerAddress.IndexOf(pattern);
            if (index >= 0)
            {
                // Find the start of the component
                int startIndex = index;

                // Extract value after the pattern
                int valueStartIndex = index + pattern.Length;
                while (valueStartIndex < address.Length && char.IsWhiteSpace(address[valueStartIndex]))
                    valueStartIndex++;

                // Find the end of this component (next pattern or end of string)
                int endIndex = address.Length;
                foreach (string otherPattern in new[] { "thôn", "ấp", "xã", "phường", "phư", "huyện", "quận", "tỉnh", "thành phố", "thành" })
                {
                    if (otherPattern == pattern)
                        continue;

                    int otherIndex = lowerAddress.IndexOf(otherPattern, valueStartIndex);
                    if (otherIndex > 0 && otherIndex < endIndex)
                        endIndex = otherIndex;
                }

                // Extract and format the component
                string value = address.Substring(valueStartIndex, endIndex - valueStartIndex).Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    // Capitalize the first letter
                    string capitalizedPattern = char.ToUpper(pattern[0]) + pattern.Substring(1);
                    return capitalizedPattern + " " + value;
                }
            }
        }

        return "";
    }
}
