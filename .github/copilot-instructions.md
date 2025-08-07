# Lyre - Chrome Native Messaging for .NET

Lyre is a .NET library that implements the Chrome Native Messaging protocol, allowing .NET applications to communicate with Chrome browser extensions. The repository includes the core library and a console test application with a corresponding Chrome extension for demonstration.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Bootstrap and Build
- The project builds successfully with .NET 8+ (though CI uses .NET Core 3.1.100).
- `dotnet build src/Lyre.sln --configuration Release` -- takes 45 seconds. NEVER CANCEL. Set timeout to 180+ seconds.
- `dotnet build src/Lyre.sln --configuration Debug` -- takes 2-3 seconds. Set timeout to 60+ seconds.
- `dotnet pack --configuration Release src/Lyre.sln` -- takes 2-3 seconds for packing after build.

### Clean Build
- `dotnet clean src/Lyre.sln` -- may show warnings/errors but cleans successfully. Takes ~1 second.
- For a completely clean rebuild, run clean followed by build commands above.

### Cross-Platform Builds
- Default console test app targets `win7-x64` (Windows-only executable).
- For Linux testing: `dotnet build src/Lyre.ConsoleTest --configuration Release --runtime linux-x64`
- The core library (Lyre) is cross-platform targeting netstandard1.3 and netstandard2.0.

## Testing and Validation

### No Unit Tests
- This project does not contain traditional unit tests.
- Use the console test application for validation instead.

### Native Messaging Functionality Test
- Build the console test app for your platform first.
- Create a Python test script to validate native messaging protocol:
```python
#!/usr/bin/env python3
import json
import struct
import subprocess

def send_message(message):
    encoded_message = json.dumps(message).encode('utf-8')
    message_length = len(encoded_message)
    length_header = struct.pack('<I', message_length)
    return length_header + encoded_message

def test_console_app():
    exe_path = "path/to/Lyre.ConsoleTest"  # Update with actual path
    test_message = {"value": "Hello from test", "dateTime": "2024-01-01T12:00:00"}
    binary_message = send_message(test_message)
    
    process = subprocess.Popen([exe_path], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    stdout, stderr = process.communicate(input=binary_message, timeout=10)
    
    # Parse and validate response
    if len(stdout) >= 4:
        response_length = struct.unpack('<I', stdout[:4])[0]
        if len(stdout) >= 4 + response_length:
            response_json = stdout[4:4+response_length].decode('utf-8')
            response = json.loads(response_json)
            print(f"Success! Received: {response}")
            return True
    return False

if __name__ == "__main__":
    test_console_app()
```

### Manual Validation Steps
- ALWAYS test native messaging functionality after making changes to the core library.
- Build both Debug and Release configurations to ensure no configuration-specific issues.
- Test cross-platform compatibility if making runtime-specific changes.

## Build Warnings and Known Issues

### Expected Warnings
- `NETSDK1138`: Target framework 'netcoreapp2.0' is out of support - EXPECTED, project uses older framework
- `NU1903`: Package 'Newtonsoft.Json' 10.0.3 has known high severity vulnerability - KNOWN ISSUE, older dependency version
- `NETSDK1201`: RuntimeIdentifier no longer produces self-contained app by default - EXPECTED with .NET 8

### Resolving Build Issues
- If you encounter "Assets file doesn't have a target" errors, run: `dotnet restore src/Lyre.sln`
- Clean builds may show errors but typically still work for cleaning files.

## Key Projects and Files

### Core Library (`src/Lyre/`)
- **Lyre.csproj**: Main library project targeting netstandard1.3/2.0
- **NativeMessagingHost.cs**: Core implementation of Chrome Native Messaging protocol
- **NativeMessagingEnvironment.cs**: Helper methods for console output management
- Multi-targets: netstandard1.3 and netstandard2.0 for broad compatibility

### Console Test Application (`src/Lyre.ConsoleTest/`)
- **Lyre.ConsoleTest.csproj**: Console app demonstrating library usage
- **Program.cs**: Main program that echoes messages back through native messaging
- **Message.cs**: Data model for test messages
- **nativeMessagingManifest.json**: Chrome native messaging manifest
- **Extension/**: Complete Chrome extension for testing communication

### Chrome Extension (`src/Lyre.ConsoleTest/Extension/`)
- **manifest.json**: Chrome extension manifest (version 2)
- **background.js**: Background script handling native messaging communication
- **popup.html/js**: Extension UI for testing communication
- Extension ID: doennphhmchjhmplpcbhlkfmpifobfjf (in manifest)

## GitHub Workflows

### CI Build (.github/workflows/dotnetcore.yml)
- Runs on every push using .NET Core 3.1.100 on ubuntu-latest
- Command: `dotnet build src/Lyre.sln --configuration Release`

### Publishing (.github/workflows/publish.yml)
- Runs on tag pushes for NuGet package publishing
- Builds, packs, and publishes to NuGet using .NET Core 3.1.100

## Common File Operations

### Repository Structure
```
/
├── .github/workflows/    # GitHub Actions workflows
├── src/
│   ├── Lyre.sln         # Main solution file
│   ├── Lyre/            # Core library project
│   └── Lyre.ConsoleTest/ # Test console app + Chrome extension
├── README.md            # Project documentation
└── LICENSE              # MIT license
```

### Dependencies
- **Core Library**: Newtonsoft.Json 10.0.3 (has known security vulnerability)
- **Console Test**: References core library project
- **Runtime Requirements**: .NET Standard 1.3+ or .NET Core 2.0+

## Chrome Extension Installation (Windows Only)
- The console test app includes a registry file: `install-native-messaging.reg`
- Native messaging manifest points to Windows-specific exe path
- Extension can be loaded in Chrome developer mode from `src/Lyre.ConsoleTest/Extension/`

## Debugging and Development Tips
- Use `NativeMessagingEnvironment.RedirectConsoleOutputToDebugStream()` to avoid breaking native messaging pipe
- Console.WriteLine calls will interfere with stdin/stdout communication
- Test with both Windows and Linux builds if making cross-platform changes
- The Chrome extension background script logs communication to browser console when `isLoggingEnabled = true`