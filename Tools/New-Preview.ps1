$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# 외부 게임 그림이나 글꼴 파일을 포함하지 않고 도형과 시스템 글꼴로 대표 이미지를 만듭니다.
$bitmap = [System.Drawing.Bitmap]::new(960, 540)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$background = [System.Drawing.Color]::FromArgb(23, 28, 32)
$graphics.Clear($background)
$white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(236, 237, 232))
$gray = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(140, 145, 149))
$panel = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(40, 46, 52))
$border = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(105, 112, 117), 2)
$title = [System.Drawing.Font]::new('Segoe UI', 36, [System.Drawing.FontStyle]::Bold)
$label = [System.Drawing.Font]::new('Malgun Gothic', 21)
$small = [System.Drawing.Font]::new('Malgun Gothic', 15)
try {
    $graphics.DrawString('Can More Implant', $title, $white, 34, 24)
    $graphics.DrawString('RimWorld 1.7  |  Health tab extension', $small, $gray, 38, 88)
    $graphics.FillRectangle($panel, 32, 151, 896, 340)
    $graphics.DrawRectangle($border, 32, 151, 896, 340)
    $graphics.DrawString('개요       수술       임플란트', $label, $white, 53, 160)
    $graphics.DrawLine($border, 48, 205, 912, 205)
    $graphics.DrawString('왼쪽 눈', $label, $white, 55, 224)
    $graphics.DrawString('생체공학 눈', $label, $white, 330, 224)
    $graphics.DrawString('초월공학 눈', $label, $gray, 330, 268)
    $graphics.DrawString('오른쪽 팔', $label, $white, 55, 323)
    $graphics.DrawString('의수', $label, $gray, 330, 323)
    $graphics.DrawString('생체공학 팔', $label, $gray, 330, 367)
    $graphics.DrawString('초월공학 팔', $label, $gray, 330, 411)
    $graphics.DrawString('Installed + available candidates  |  Illustrative preview', $small, $gray, 38, 503)
    $bitmap.Save((Join-Path $PSScriptRoot '..\About\Preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $graphics.Dispose(); $bitmap.Dispose()
    $white.Dispose(); $gray.Dispose(); $panel.Dispose(); $border.Dispose()
    $title.Dispose(); $label.Dispose(); $small.Dispose()
}
