# tink-fsharp

tink data-flow node frame protocol — F# (.NET) module (no dependencies).
Universal and language-agnostic: any component that obeys the frame protocol
can join a tink pipeline.

```
帧 = [ len: u32 BE ][ payload: len 字节 ][ crc: u32 BE ]
len = payload 字节数
crc = CRC32-IEEE(payload)（多项式 0xEDB88320）
```

Mirrors `std/tink.tie` (tie standard library) and the other-language tink
libraries; pure functions over `byte[]`, IO (stdin/stdout) left to the caller.
Names follow the F# convention (camelCase: `frameEncode`, `frameNext`,
`frameSkip`).

## API (`module Tink`)

| function | description |
| --- | --- |
| `crc32 (data: byte[]) : uint32` | CRC32-IEEE over a byte array. Check vector: `crc32(ascii"123456789") = 0xCBF43926u` |
| `frameEncode (payload: byte[]) : byte[]` | encode a payload into a full frame `[len][payload][crc]` |
| `frameNext (bytes, pos) : (byte[] * int) option` | parse one frame at `pos`, verify CRC; `Some(payload, next)` on success, `None` on out-of-bounds / mismatch |
| `frameSkip (bytes, pos) : int option` | skip one frame at `pos` without copying or verifying; `None` on out-of-bounds |

## Usage

```fsharp
let frame = Tink.frameEncode [| 1uy; 2uy; 3uy |]
let got = Tink.frameNext frame 0   // (byte[] * int) option
```

## Build & test

```bash
dotnet run        # runs the bundled unit tests
```

## Cross-language

tink 帧协议各语言实现（API 语义与校验向量一致）：

| language | library |
| --- | --- |
| tie | `std/tink.tie` |
| Rust | `tink-rust`（tink crate） |
| C | `tink-c`（`tink.h` + `tink.c`） |
| Python | `tink-python`（`tink.py`） |
| JavaScript | `tink-js`（`tink.js` + `tink.d.ts`） |
| C++ | `tink-cpp`（`tink.hpp`） |
| Java | `tink-java`（`org.tielang.tink`） |
| C# | `tink-csharp`（namespace `Tink`） |
| Go | `tink-go`（package `tink`） |
| Zig | `tink-zig`（`tink.zig`） |
| Lua | `tink-lua`（`tink.lua`） |
| GDScript | `tink-godot`（`tink.gd`） |
| F# | this module（`tink-fsharp`） |

## License

本仓库使用 **Tie Public License v1.2 (TPL 1.2)**，完整文本见 [LICENSE](LICENSE)。
This repository is distributed under the **Tie Public License v1.2 (TPL 1.2)** — see [LICENSE](LICENSE) for the full text.