using System.Text.Json;
namespace Flake.Settings;
public sealed class JsonSettingsStore
{
 private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
 private readonly string _directory;
 public JsonSettingsStore(string? directory = null) => _directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Flake");
 public async Task<T> LoadAsync<T>(string fileName, T defaults, CancellationToken ct = default)
 { var path = Resolve(fileName); try { if (!File.Exists(path)) return defaults; await using var stream = File.OpenRead(path); return await JsonSerializer.DeserializeAsync<T>(stream, Options, ct) ?? defaults; } catch (JsonException) { return defaults; } catch (IOException) { return defaults; } }
 public async Task SaveAsync<T>(string fileName, T value, CancellationToken ct = default)
 { Directory.CreateDirectory(_directory); var path = Resolve(fileName); var temp = path + ".tmp"; await using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) await JsonSerializer.SerializeAsync(stream, value, Options, ct); File.Move(temp, path, true); }
 private string Resolve(string name) { if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name)) throw new ArgumentException("A settings file name must not contain a path.", nameof(name)); return Path.Combine(_directory, name); }
}
