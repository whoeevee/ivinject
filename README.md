<img src="Images/Banner.png" width="256"/>

An iOS app injector and signer, the most demure in my opinion. The point is, while developing, I was primarily accommodating myself and made the injector exactly as I wanted to see it. Well, I'm happy to use it. The EeveeSpotify official IPAs are also created with ivinject starting from version 5.8.

Thus, feature requests and bug reports are not accepted. You probably would like to use [cyan](https://github.com/asdfzxcvbn/pyzule-rw) instead, which remains highly recommended for widespread public use due to its support. In fact, many things are quite similar: both ivinject and cyan can inject tweaks, frameworks, and bundles, fix dependencies, fake sign, etc. However, there are some crucial differences.

## The Demureness
- **ivinject is an entirely different project, written in C# with .NET 9. The code architecture and quality are significantly better. Compiled with NativeAOT, it produces native binaries, offering incredible speed and low resource usage, without needing anything like venv to run.**

- ivinject is not just an injector but also a signer. You can specify a code signing identity and a file with entitlements that will be written into the main executables. It signs the code properly according to Apple's technotes, passing codesign verification with the `--strict` option. It only supports developer certificates (.p12 and .mobileprovision).

- ivinject does not and won’t support anything except for macOS — I couldn’t care less about other platforms.

- Some more differences like ivinject supports more bundle types for signing and package modifications, such as Extensions or Watch; forcefully thins binaries; does not and won't support configuration files, etc.

## Prerequisites
* Make sure Xcode is installed
* Install insert-dylib (`brew install --HEAD samdmarshall/formulae/insert-dylib`)
* Copy the contents of `KnownFrameworks` to `~/.ivinject`
* For code signing, the identity needs to be added to Keychain, and the provisioning profile must be installed on the device (you can also add it to the app package by specifying `embedded.mobileprovision` in items)