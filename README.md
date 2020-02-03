# Fingercrypt
[![NuGet version ()](https://img.shields.io/nuget/v/fingercrypt?style=for-the-badge)](https://www.nuget.org/packages/Fingercrypt/)
[![License ()](https://img.shields.io/github/license/Geometrically/Fingercrypt?style=for-the-badge)](https://opensource.org/licenses/MIT)
[![Downloads ()](https://img.shields.io/nuget/dt/Fingercrypt?style=for-the-badge)](https://www.nuget.org/packages/Fingercrypt/)

Fingercrypt is a simple solution for secure fingerprint/biometric hashing which also supports comparison checks using permutations.

# Contents
- [Introduction](#introduction)
- [How To Use](#how-to-use)


# Introduction 

Fingercrypt allows for the secure storage of fingerprints while allowing for easy comparison checks!
It uses OpenCV in order to process the image. Image processing is done in four simple steps. First,
the image converted to a black and white form, with the lines of the fingerprint being white and the 
rest being black. Then, the finger's ridges are outlined using the Cv2 canny algorithm. After that,
the region of interest is captured to crop out all unneccasary things that are not the actual fingerpint.
After that, the Hough Lines Transform is applied to get the lines from the image, which are then consequently
hashed.

Here is the process visualized:

![Step 1](https://i.imgur.com/lie0p58.jpg)
![Step 2](https://i.imgur.com/iGq4Zmi.png)
![Step 3](https://i.imgur.com/jdhZEq0.png)

Step three is not that visible, sorry about that!

# How to Use

Fingercrypt comes with 2 main methods, `HashFingerprint` and `CheckFingerprint`

`string HashFingerprint(string imgPath, bool inverseColors)`

- `imgPath` - The relative path to the image
- `inverseColors` - True if the fingerprint's lines are darker than the finger


`byte[] HashFingerprint(LineSegmentPoint[] lines, int linesPerChunk=1, int chunkSize=16)`

- `lines` - The OpenCV2 Lines of the fingerprint
- `linesPerChunk` - How many lines are stored in every SHA-512 Hash. Tradeoff between computation time during checking and storage space
- `chunkSize` - [NOT IMPLEMENTED] The length (in bytes) of each SHA-512 Hash


`bool CheckFingerprint(string hashedLines, LineSegmentPoint[] lines, float chunkPercentThresold, int chunkLength = 128, int linesPerChunk = 1, int allowedVariation = 1)`

- `hashedLines` - The fingerprint hash
- `chunkPercentThresold` - The minimum percentage of chunks that match in order for the hashes to be considered matching
- `lines` - The lines that are being checked against the fingerprint hash
- `chunkLength` - [NOT IMPLEMENTED] The length (in bytes) of each SHA-512 Hash
- `linesPerChunk` - How many lines are stored in every SHA-512 Hash. Tradeoff between computation time during checking and storage space
- `allowedVariation` - The allowed variation of each point in a line that can still count as a match

