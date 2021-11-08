  Function Read-Bitmap
  {
    param(
        [String]$path
    )
    begin {
        [void] [System.Reflection.Assembly]::LoadWithPartialName("System.drawing")
        $BitMap = [System.Drawing.Bitmap]::FromFile($path)

        $w = $BitMap.Width
        $h = $BitMap.Height

        $myArray = New-Object int[] 128;

        Foreach($x in (0..($w-1)))
        {
            Foreach($y in (0..($h-1)))
            {
                $Pixel = $BitMap.GetPixel($X,$Y)      
                if($Pixel.R -ne 0) {
                    $ind = ($y % 8) + [math]::floor($x / 8) * 8 + ([math]::floor($y / 8) * 64);
                    $myArray[$ind] = $myArray[$ind] -bor (1 -shl ($x % 8))
                }  
                
            }
        }

        Write-Host
        $myArray | %{ Write-Host "$_, " -NoNewline }
    }
}

Read-Bitmap C:\todel\panda.bmp
Read-Bitmap C:\todel\panda2b.bmp
