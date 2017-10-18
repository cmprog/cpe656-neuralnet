using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

internal sealed class ResolvedSampleWriter : IDisposable
{
    private readonly Stream mStream;
    private readonly StreamWriter mStreamWriter;
    private readonly DirectoryInfo mImageDirectory;
    private readonly IdGenerator mImageIdGenerator;
    
    private CameraViewportCalculator mCameraViewportCalculator;

    private bool mColumnHeadersWritten = false;

    private const string sDelimiter = ",";

    public ResolvedSampleWriter(FileInfo file, DirectoryInfo imageDirectory, Camera referenceCamera)
    {
        this.mStream = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
        this.mStreamWriter = new StreamWriter(this.mStream, Encoding.UTF8);
        this.mImageDirectory = imageDirectory;
        this.mImageIdGenerator = new IdGenerator();

        this.mCameraViewportCalculator = new CameraViewportCalculator(referenceCamera);
    }

    public void Write(ResolvedSample sample)
    {
        // First, write out the image
        var lImageId = this.mImageIdGenerator.Next();
        var lImageFileName = string.Concat(lImageId, ".jpg"); // We're always working with jpg files
        var lImageFilePath = Path.Combine(this.mImageDirectory.FullName, lImageFileName);
        File.WriteAllBytes(lImageFilePath, sample.TelemetryEventArgs.Image);

        if (!this.mColumnHeadersWritten)
        {
            // The beginning of the file needs headers
            this.mStreamWriter.Write("Image");
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write("Position (X)");
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write("Position (Y)");
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write("Size (Width)");
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write("Size (Height)");
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write("Was Detected");

            this.mColumnHeadersWritten = true;
        }

        this.mStreamWriter.WriteLine();

        this.mStreamWriter.Write(lImageFilePath);
        this.mStreamWriter.Write(sDelimiter);

        var lViewportBounds = this.mCameraViewportCalculator.GetBounds(sample.GameObject);

        if (lViewportBounds.IsInBounds)
        {
            this.mStreamWriter.Write(lViewportBounds.X);
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write(lViewportBounds.Y);
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write(lViewportBounds.Width);
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write(lViewportBounds.Height);
        }
        else
        {
            // No bounding information to write, so just leave empty
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write(sDelimiter);
            this.mStreamWriter.Write(sDelimiter);
        }

        this.mStreamWriter.Write(sDelimiter);
        this.mStreamWriter.Write(sample.WasDetected);
    }

    public void Dispose()
    {
        if (this.mStreamWriter != null)
        {
            // Disposing the writer disposes the file stream
            this.mStreamWriter.Dispose();
        }
    }
}