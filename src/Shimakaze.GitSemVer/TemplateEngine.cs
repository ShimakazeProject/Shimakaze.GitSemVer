using System.Text;

namespace Shimakaze.GitSemVer;

internal sealed class TemplateEngine(IReadOnlyDictionary<string, string?> values, IReadOnlyDictionary<string, string> templates)
{
    private readonly Dictionary<string, string?> _resolved = [with(StringComparer.OrdinalIgnoreCase)];
    private readonly List<string> _resolving = [];

    public string? Resolve(string name)
    {
        if (_resolved.TryGetValue(name, out var cached))
            return cached;

        if (templates.TryGetValue(name, out var template))
        {
            int cycleStart = _resolving.FindIndex(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
            if (cycleStart >= 0)
            {
                string cycle = string.Join(" -> ", _resolving.Skip(cycleStart).Append(name));
                throw new InvalidOperationException($"Template reference cycle detected: {cycle}.");
            }

            _resolving.Add(name);
            try
            {
                string rendered = Render(template);
                _resolved.Add(name, rendered);
                return rendered;
            }
            finally
            {
                _resolving.RemoveAt(_resolving.Count - 1);
            }
        }

        if (values.TryGetValue(name, out var value))
            return value;

        throw new FormatException($"Unknown template variable '{name}'.");
    }

    private string Render(string template)
    {
        StringBuilder result = new(template.Length);

        for (int position = 0; position < template.Length;)
        {
            if (template[position] != '{')
            {
                result.Append(template[position++]);
                continue;
            }

            int closingBrace = FindClosingBrace(template, position);
            string expression = template[(position + 1)..closingBrace];
            result.Append(Evaluate(expression));
            position = closingBrace + 1;
        }

        return result.ToString();
    }

    private string Evaluate(string expression)
    {
        int conditionSeparator = FindTopLevelSeparator(expression, '?');
        if (conditionSeparator < 0)
        {
            string name = expression.Trim();
            if (name.Length == 0)
                throw new FormatException("Template variable names cannot be empty.");

            return Resolve(name) ?? string.Empty;
        }

        string conditionName = expression[..conditionSeparator].Trim();
        if (conditionName.Length == 0)
            throw new FormatException("Template conditions must specify a variable name.");

        int branchSeparator = FindTopLevelSeparator(expression, ':', conditionSeparator + 1);
        if (branchSeparator < 0)
            throw new FormatException($"Template condition '{conditionName}' is missing the ':' branch separator.");

        string? conditionValue = Resolve(conditionName);
        string branch = conditionValue is not null && conditionValue.Length > 0
            ? expression[(conditionSeparator + 1)..branchSeparator]
            : expression[(branchSeparator + 1)..];

        return Render(branch.Trim());
    }

    private static int FindClosingBrace(string template, int openingBrace)
    {
        int depth = 0;
        for (int position = openingBrace; position < template.Length; position++)
        {
            if (template[position] == '{')
                depth++;
            else if (template[position] == '}')
            {
                depth--;
                if (depth == 0)
                    return position;
            }
        }

        throw new FormatException($"Unclosed template expression at position {openingBrace}.");
    }

    private static int FindTopLevelSeparator(string expression, char separator, int start = 0)
    {
        int depth = 0;
        for (int position = start; position < expression.Length; position++)
        {
            if (expression[position] == '{')
                depth++;
            else if (expression[position] == '}')
                depth--;
            else if (expression[position] == separator && depth == 0)
                return position;
        }

        return -1;
    }
}
