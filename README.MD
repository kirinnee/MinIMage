# MinIMage

.NET Standard Wrapper to allow for programmatic call for both OptiPNG and JPEGOptim to compress image. 
There is an addition nQuant PNG Quantizer which does not require the binary to be installed.

All Compression methods are *lossy*.

# Prerequisite

*All dependency needs to be avaliable via `${PATH}`*

[PNGQuant](https://pngquant.org/) - PNG Compression Tool. Minimum version 2.12.0

[JPEGOptim](https://github.com/tjko/jpegoptim) - JPEG Compression Tool. Minimum version 1.4.4

**Ensure Binary version are up to date!**

Check PNGQuant version :
```shell
$ pngquant --verbose
```

Check JPEGOptim version :
```shell
$ jpegoptim --help
```

# Getting Started

Install via .NET CLI
```powershell
$ dotnet add package Kirinnee.Minimage 
```

or 

Install via NuGet Package Manager
```powershell
PM> Install-Package Kirinnee.Minimage 
```

# Features

## JPEGOptim

### Without Settings
```cs
///Invoke the compressor
Compressor jpegOptim = new JpegOptim();
//Image bytes
byte[] image = File.ReadAllBytes("path");
//Compress bytes
byte[] compressed = await jpegOptim.Compress(image);
```

### With Settings
```cs
//Options
var options = new JpegOptimOptions(){
    Quality = 80, //Maximum quality, default 76
    StripAll = true, //Strip all metadata and markers, default true
    Overwrite = false, //Overwrite target file if it already exist, default false
    Progressive = false, //Make the image progressive. Will make the image not progessive if false. default true,
    Force = true //Compress regardless of 
};
///Invoke the compressor
Compressor jpegOptim = new JpegOptim(options);
//Image bytes
byte[] image = File.ReadAllBytes("path");
//Compress bytes
byte[] compressed = await jpegOptim.Compress(image);
```

## PNGQuant

### Without Settings
```cs
///Invoke the compressor
Compressor pngQuant = new PngQuant();
//Image bytes
byte[] image = File.ReadAllBytes("path");
//Compress bytes
byte[] compressed = await pngQuant.Compress(image);
```

### With Settings
```cs
//Options
var options = new PngQuantOptions(){
    QualityMinMax = (65,80), //Minimum = 65, Maximum = 80. Default null
    Spped = 1, //Value between 1 and 11. default 3. 
    IEBug = false, //Attempt to fix iebug. default false.
    Bit = 256 //bit-rate. default 256
};
///Invoke the compressor
Compressor pngQuant = new PngQuant(options);
//Image bytes
byte[] image = File.ReadAllBytes("path");
//Compress bytes
byte[] compressed = await pngQuant.Compress(image);
```

## WuQuant

High-speed quantizer, using  Xialoin Wu's fast optimal color quantizer with nQuant. Image quality drops quite significantly, but runs really fast.

```cs
///Invoke the compressor
Compressor wuQuant = new WuQuant();
//Image bytes
byte[] image = File.ReadAllBytes("path");
//Compress bytes
byte[] compressed = await wuQuant.Compress(image);
```

## Contributing
Please read [CONTRIBUTING.md](CONTRIBUTING.MD) for details on our code of conduct, and the process for submitting pull requests to us.

## Versioning 
We use [SemVer](https://semver.org/) for versioning. For the versions available, see the tags on this repository.

## Authors
* [kirinnee](mailto:kirinnee@gmail.com) 

## License
This project is licensed under MIT - see the [LICENSE.md](LICENSE.MD) file for details