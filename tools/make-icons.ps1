#!/usr/bin/env pwsh
# tools/make-icons.ps1
# Generates 32×32 ICO files for RDPConf and RDPCheck.
# Requires .NET (Windows / .NET 4.5+). Run from repo root:
#   .\tools\make-icons.ps1
# Output:
#   src-csharp/RDPConf/app.ico
#   src-csharp/RDPCheck/app.ico

#Requires -Version 5

Add-Type -AssemblyName System.Drawing

$REPO = Split-Path -Parent $PSScriptRoot

function New-AppIcon {
    param(
        [string]$OutPath,
        [System.Drawing.Color]$BackColor,
        [string]$Letter,
        [System.Drawing.Color]$ForeColor = [System.Drawing.Color]::White
    )

    $sz = 32
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz,
               [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode    = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Rounded-rectangle background
    $bg = New-Object System.Drawing.SolidBrush($BackColor)
    $r  = 5   # corner radius
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $r*2, $r*2, 180, 90)
    $path.AddArc($sz - $r*2, 0, $r*2, $r*2, 270, 90)
    $path.AddArc($sz - $r*2, $sz - $r*2, $r*2, $r*2, 0, 90)
    $path.AddArc(0, $sz - $r*2, $r*2, $r*2, 90, 90)
    $path.CloseFigure()
    $g.FillPath($bg, $path)

    # Centered letter
    $font = New-Object System.Drawing.Font(
                "Segoe UI", 18, [System.Drawing.FontStyle]::Bold,
                [System.Drawing.GraphicsUnit]::Pixel)
    $fg  = New-Object System.Drawing.SolidBrush($ForeColor)
    $sf  = New-Object System.Drawing.StringFormat
    $sf.Alignment     = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = New-Object System.Drawing.RectangleF(0, 0, $sz, $sz)
    $g.DrawString($Letter, $font, $fg, $rect, $sf)

    $g.Dispose()

    # --- Encode as PNG then wrap in ICO on-disk ---
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $png = $ms.ToArray()
    $ms.Dispose()
    $bmp.Dispose()

    # ICO file format:
    #   ICONDIR        6 bytes  : reserved(2) type=1(2) count=1(2)
    #   ICONDIRENTRY  16 bytes  : w(1),h(1),colors(1),resv(1),planes(2),bits(2),size(4),offset(4)
    #   PNG data
    $null = New-Item -Force -ItemType File $OutPath
    $stream = [System.IO.File]::Open($OutPath,
                  [System.IO.FileMode]::Create,
                  [System.IO.FileAccess]::Write)
    $w = New-Object System.IO.BinaryWriter($stream,
             [System.Text.Encoding]::ASCII, $false)

    # ICONDIR
    $w.Write([uint16]0)       # reserved
    $w.Write([uint16]1)       # type = ICON
    $w.Write([uint16]1)       # image count

    # ICONDIRENTRY
    $w.Write([byte]$sz)       # width  (0 = 256)
    $w.Write([byte]$sz)       # height
    $w.Write([byte]0)         # color count (0 = true-color)
    $w.Write([byte]0)         # reserved
    $w.Write([uint16]1)       # color planes
    $w.Write([uint16]32)      # bits per pixel
    $w.Write([uint32]$png.Length)   # image data size
    $w.Write([uint32]22)      # offset to image data (6 + 16 = 22)

    # PNG bytes
    $w.Write($png)

    $w.Close()
    $stream.Close()

    Write-Host "  Created: $OutPath"
}

Write-Host "Generating application icons..."

New-AppIcon -OutPath "$REPO\src-csharp\RDPConf\app.ico" `
            -BackColor ([System.Drawing.Color]::FromArgb(0, 84, 166)) `
            -Letter    "C"

New-AppIcon -OutPath "$REPO\src-csharp\RDPCheck\app.ico" `
            -BackColor ([System.Drawing.Color]::FromArgb(16, 124, 16)) `
            -Letter    "K"

Write-Host "Done."
