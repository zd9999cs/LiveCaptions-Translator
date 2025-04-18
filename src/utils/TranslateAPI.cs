using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.utils
{
    public static class TranslateAPI
    {
        /*
         * The key of this field is used as the content for `translateAPIBox` in the `SettingPage`.
         * If you'd like to add a new API, please insert the key-value pair here.
         */
        public static readonly Dictionary<string, Func<string, CancellationToken, Task<string>>>
            TRANSLATE_FUNCTIONS = new()
        {
            { "Google", Google },
            { "Google2", Google2 },
            { "Ollama", Ollama },
            { "OpenAI", OpenAI },
            { "DeepL", DeepL },
            { "OpenRouter", OpenRouter },
            { "Youdao", Youdao },
            { "MTranServer", MTranServer },
            { "Gemini", Gemini }, // Add Gemini API
        };

        public static Func<string, CancellationToken, Task<string>> TranslateFunction
        {
            get => TRANSLATE_FUNCTIONS[Translator.Setting.ApiName];
        }
        public static string Prompt
        {
            get => Translator.Setting.Prompt;
        }

        private static readonly HttpClient client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public static async Task<string> OpenAI(string text, CancellationToken token = default)
        {
            var config = Translator.Setting.CurrentAPIConfig as OpenAIConfig;
            string language = config.SupportedLanguages.TryGetValue(Translator.Setting.TargetLanguage, out var langValue)
                ? langValue
                : Translator.Setting.TargetLanguage;
            var requestData = new
            {
                model = config?.ModelName,
                messages = new BaseLLMConfig.Message[]
                {
                    new BaseLLMConfig.Message { role = "system", content = string.Format(Prompt, language)},
                    new BaseLLMConfig.Message { role = "user", content = $"🔤 {text} 🔤" }
                },
                temperature = config?.Temperature,
                max_tokens = 64,
                stream = false
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config?.ApiKey}");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(TextUtil.NormalizeUrl(config?.ApiUrl), content, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<OpenAIConfig.Response>(responseString);
                return responseObj.choices[0].message.content;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }

        public static async Task<string> Ollama(string text, CancellationToken token = default)
        {
            var config = Translator.Setting?.CurrentAPIConfig as OllamaConfig;
            var apiUrl = $"http://localhost:{config.Port}/api/chat";
            string language = config.SupportedLanguages.TryGetValue(Translator.Setting.TargetLanguage, out var langValue)
                ? langValue
                : Translator.Setting.TargetLanguage;

            var requestData = new
            {
                model = config?.ModelName,
                messages = new BaseLLMConfig.Message[]
                {
                    new BaseLLMConfig.Message { role = "system", content = string.Format(Prompt, language)},
                    new BaseLLMConfig.Message { role = "user", content = $"🔤 {text} 🔤" }
                },
                temperature = config?.Temperature,
                max_tokens = 64,
                stream = false
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Clear();

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(apiUrl, content, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<OllamaConfig.Response>(responseString);
                return responseObj.message.content;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }

        private static async Task<string> Google(string text, CancellationToken token = default)
        {
            var language = Translator.Setting?.TargetLanguage;

            string encodedText = Uri.EscapeDataString(text);
            var url = $"https://clients5.google.com/translate_a/t?" +
                      $"client=dict-chrome-ex&sl=auto&" +
                      $"tl={language}&" +
                      $"q={encodedText}";

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();

                var responseObj = JsonSerializer.Deserialize<List<List<string>>>(responseString);

                string translatedText = responseObj[0][0];
                return translatedText;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }

        private static async Task<string> Google2(string text, CancellationToken token = default)
        {
            string apiKey = "AIzaSyA6EEtrDCfBkHV8uU2lgGY-N383ZgAOo7Y";
            var language = Translator.Setting?.TargetLanguage;
            string strategy = "2";

            string encodedText = Uri.EscapeDataString(text);
            string url = $"https://dictionaryextension-pa.googleapis.com/v1/dictionaryExtensionData?" +
                         $"language={language}&" +
                         $"key={apiKey}&" +
                         $"term={encodedText}&" +
                         $"strategy={strategy}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-referer", "chrome-extension://mgijmajocgfcbeboacabfgobmjgjcoja");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                using var jsonDoc = JsonDocument.Parse(responseBody);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("translateResponse", out JsonElement translateResponse))
                {
                    string translatedText = translateResponse.GetProperty("translateText").GetString();
                    return translatedText;
                }
                else
                    return "[Translation Failed] Unexpected API response format";
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }

        public static async Task<string> OpenRouter(string text, CancellationToken token = default)
        {
            var config = Translator.Setting.CurrentAPIConfig as OpenRouterConfig;
            var language = config?.SupportedLanguages[Translator.Setting.TargetLanguage];
            var apiUrl = "https://openrouter.ai/api/v1/chat/completions";

            var requestData = new
            {
                model = config?.ModelName,
                messages = new[]
                {
                    new { role = "system", content = string.Format(Prompt, language)},
                    new { role = "user", content = $"🔤 {text} 🔤" }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            request.Headers.Add("Authorization", $"Bearer {config?.ApiKey}");
            request.Headers.Add("HTTP-Referer", "https://github.com/SakiRinn/LiveCaptionsTranslator");
            request.Headers.Add("X-Title", "LiveCaptionsTranslator");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return jsonResponse.GetProperty("choices")[0]
                                   .GetProperty("message")
                                   .GetProperty("content")
                                   .GetString() ?? string.Empty;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }

        public static async Task<string> DeepL(string text, CancellationToken token = default)
        {
            var config = Translator.Setting.CurrentAPIConfig as DeepLConfig;
            string language = config.SupportedLanguages.TryGetValue(Translator.Setting.TargetLanguage, out var langValue)
                ? langValue
                : Translator.Setting.TargetLanguage;
            string apiUrl = TextUtil.NormalizeUrl(config.ApiUrl);

            var requestData = new
            {
                text = new[] { text },
                target_lang = language
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {config?.ApiKey}");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(apiUrl, content, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("translations", out var translations) &&
                    translations.ValueKind == JsonValueKind.Array && translations.GetArrayLength() > 0)
                {
                    return translations[0].GetProperty("text").GetString();
                }
                return "[Translation Failed] No valid feedback";
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }


        public static async Task<string> Youdao(string text, CancellationToken token = default)
        {
            var config = Translator.Setting.CurrentAPIConfig as YoudaoConfig;
            string language = config.SupportedLanguages.TryGetValue(Translator.Setting.TargetLanguage, out var langValue)
                ? langValue : Translator.Setting.TargetLanguage;

            string salt = DateTime.Now.Millisecond.ToString();
            string sign = BitConverter.ToString(
                MD5.Create().ComputeHash(
                    Encoding.UTF8.GetBytes($"{config.AppKey}{text}{salt}{config.AppSecret}"))).Replace("-", "").ToLower();

            var parameters = new Dictionary<string, string>
            {
                ["q"] = text,
                ["from"] = "auto",
                ["to"] = language,
                ["appKey"] = config.AppKey,
                ["salt"] = salt,
                ["sign"] = sign
            };

            var content = new FormUrlEncodedContent(parameters);
            client.DefaultRequestHeaders.Clear();

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(config.ApiUrl, content, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<YoudaoConfig.TranslationResult>(responseString);

                if (responseObj.errorCode != "0")
                    return $"[Translation Failed] Youdao Error {responseObj.errorCode}";

                return responseObj.translation?.FirstOrDefault() ?? "[Translation Failed] No content";
            }
            else
            {
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
            }
        }

        public static async Task<string> MTranServer(string text, CancellationToken token = default)
        {
            var config = Translator.Setting.CurrentAPIConfig as MTranServerConfig;
            string targetLanguage = config.SupportedLanguages.TryGetValue(Translator.Setting.TargetLanguage, out var langValue)
                ? langValue
                : Translator.Setting.TargetLanguage;
            string sourceLanguage = config.SourceLanguage;
            string apiUrl = TextUtil.NormalizeUrl(config.ApiUrl);

            var requestData = new
            {
                text = text,
                to = targetLanguage,
                from = sourceLanguage
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config?.ApiKey}");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(apiUrl, content, token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.Message.StartsWith("The request"))
                    return $"[Translation Failed] {ex.Message}";
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<MTranServerConfig.Response>(responseString);
                return responseObj.result;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }

        public static async Task<string> Gemini(string text, CancellationToken token = default)
        {
            var config = Translator.Setting.CurrentAPIConfig as GeminiConfig;
            if (config == null) return "[Translation Failed] Gemini configuration is missing or invalid.";

            string language = config.SupportedLanguages.TryGetValue(Translator.Setting.TargetLanguage, out var langValue)
                ? langValue
                : Translator.Setting.TargetLanguage;

            // Construct the API URL with the API key
            string apiUrlWithKey = $"{config.ApiUrl}?key={config.ApiKey}";

            // Gemini API uses a different payload structure
            var requestData = new
            {
                contents = new List<GeminiConfig.Content>
                {
                    // Optional: Add system instructions or context if needed, Gemini uses role 'model' for its responses and 'user' for prompts typically.
                    // You might need to adjust the prompt format for Gemini.
                    new GeminiConfig.Content {
                        role = "user",
                        parts = new List<GeminiConfig.ContentPart> { new GeminiConfig.ContentPart { text = string.Format(Prompt, language) } } // System prompt/instruction
                    },
                     new GeminiConfig.Content {
                        role = "model", // Placeholder for expected model response structure if doing multi-turn
                        parts = new List<GeminiConfig.ContentPart> { new GeminiConfig.ContentPart { text = "Okay, I will translate the following text." } }
                    },
                    new GeminiConfig.Content {
                        role = "user",
                        parts = new List<GeminiConfig.ContentPart> { new GeminiConfig.ContentPart { text = $"🔤 {text} 🔤" } } // Actual text to translate
                    }
                },
                // Optional: Add generationConfig like temperature, maxOutputTokens etc.
                generationConfig = new
                {
                    temperature = config.Temperature, // Use temperature from BaseLLMConfig
                    maxOutputTokens = 128, // Increased token limit slightly
                    // candidateCount = 1 // Default is 1
                    // Move thinkingConfig inside generationConfig
                    thinkingConfig = new
                    {
                        thinkingBudget = 0
                    }
                }
                // Optional: Add safetySettings if needed
            };


            string jsonContent = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Clear previous headers if reusing client, though Gemini doesn't use Authorization Bearer token typically
            client.DefaultRequestHeaders.Clear();
            // Gemini API key is usually passed as a query parameter, not a header.

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(apiUrlWithKey, content, token);
            }
            catch (OperationCanceledException ex)
            {
                 // Check if it's a timeout (HttpRequestException with specific inner exception/message might be needed)
                if (ex.Message.Contains("The request was canceled due to the configured HttpClient.Timeout")) // More specific check
                     return $"[Translation Failed] Timeout: {ex.Message}";
                // Handle task cancellation specifically
                if (token.IsCancellationRequested)
                    return string.Empty; // Or "[Translation Cancelled]"
                // General cancellation (might wrap other exceptions)
                return $"[Translation Failed] Operation Canceled: {ex.Message}";
            }
            catch (HttpRequestException httpEx) // Catch network-related errors
            {
                 return $"[Translation Failed] Network Error: {httpEx.Message}";
            }
            catch (Exception ex) // Catch other unexpected errors
            {
                return $"[Translation Failed] Error: {ex.Message}";
            }


            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                try
                {
                    var responseObj = JsonSerializer.Deserialize<GeminiConfig.Response>(responseString);
                    // Extract text from the first candidate's content parts
                    if (responseObj?.candidates != null && responseObj.candidates.Count > 0 &&
                        responseObj.candidates[0].content?.parts != null && responseObj.candidates[0].content.parts.Count > 0)
                    {
                        // Combine text from potentially multiple parts, though usually one for simple generation
                        return string.Join("", responseObj.candidates[0].content.parts.Select(p => p.text));
                    }
                    else
                    {
                        // Handle cases where the response structure is unexpected or empty
                        return "[Translation Failed] Unexpected API response format or empty content.";
                    }
                }
                catch (JsonException jsonEx)
                {
                    return $"[Translation Failed] Error parsing response: {jsonEx.Message}";
                }
            }
            else
            {
                 // Attempt to read error details from the response body
                string errorBody = await response.Content.ReadAsStringAsync();
                return $"[Translation Failed] HTTP Error - {response.StatusCode}. Details: {errorBody}";
            }
        }
    }

    public class ConfigDictConverter : JsonConverter<Dictionary<string, TranslateAPIConfig>>
    {
        public override Dictionary<string, TranslateAPIConfig> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var configs = new Dictionary<string, TranslateAPIConfig>();
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected a StartObject token.");

            reader.Read();
            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                string key = reader.GetString();
                reader.Read();

                TranslateAPIConfig config;
                var configType = Type.GetType($"LiveCaptionsTranslator.models.{key}Config");
                if (configType != null && typeof(TranslateAPIConfig).IsAssignableFrom(configType))
                    config = (TranslateAPIConfig)JsonSerializer.Deserialize(ref reader, configType, options);
                else
                    config = (TranslateAPIConfig)JsonSerializer.Deserialize(ref reader, typeof(TranslateAPIConfig), options);

                configs[key] = config;
                reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException("Expected an EndObject token.");
            return configs;
        }

        public override void Write(
            Utf8JsonWriter writer, Dictionary<string, TranslateAPIConfig> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);

                var configType = Type.GetType($"LiveCaptionsTranslator.models.{kvp.Key}Config");
                if (configType != null && typeof(TranslateAPIConfig).IsAssignableFrom(configType))
                    JsonSerializer.Serialize(writer, kvp.Value, configType, options);
                else
                    JsonSerializer.Serialize(writer, kvp.Value, typeof(TranslateAPIConfig), options);
            }
            writer.WriteEndObject();
        }
    }
}
