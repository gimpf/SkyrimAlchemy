module String

let parseDouble x = System.Double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)

let parseInt x = System.Int32.Parse(x, System.Globalization.CultureInfo.InvariantCulture)

let parseBool x = System.Boolean.Parse(x)

let startsWith x (str : string) = str.StartsWith(x)

let removeAtFront x (str : string) = if str.StartsWith(x) then str.Substring(x.Length) else str

let removeAll x (str : string) = str.Replace(x, System.String.Empty)

let trim (str : string) = str.Trim()

let NewLineChars = System.Environment.NewLine.ToCharArray()

let chomp (str : string) = str.Trim(NewLineChars)

let split separators (str : string) = str.Split(separators)
