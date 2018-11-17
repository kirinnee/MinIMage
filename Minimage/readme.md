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

# Documentation

The Full Documentation will not be placed here because Nuget has a 8000 byte limit on documentation :<

The documentation isn't that long...

[Full Documentation](https://github.com/kirinnee/MinIMage#MinIMage)