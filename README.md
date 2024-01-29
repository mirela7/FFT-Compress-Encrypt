# FFT-Compress-Encrypt
PI project

## FFT and DCTCompress


| Initial Image | Kept FFT Precentage | Decompressed image | Compression Rate |
|---|---|---|---|
|<img src="./data/grayscale-lamp.png" width="150px" /> | 1% | <img src="./data/lamp-test_bw_compressed_decompressed_01.bmp" width="150px" /> | Original size: 16MB <br> Compressed binary: 838.872KB |
|<img src="./data/grayscale-lamp.png" width="150px" /> | 2% | <img src="./data/lamp-test_bw_compressed_decompressed_02.bmp" width="150px" /> | Original size: 16MB <br> Compressed binary: 1.59MB |
|<img src="./data/grayscale-lamp.png" width="150px" /> | 5% | <img src="./data/lamp-test_bw_compressed_decompressed_05.bmp" width="150px" /> | Original size: 16MB <br> Compressed binary: 4MB |
|<img src="./data/lamp-test.bmp" width="150px" /> | 1% | <img src="./data/lamp-test_compressed_decompressed_01.bmp" width="150px" /> | Original size: 16MB <br> Compressed binary: 2.39MB |
|<img src="./data/lamp-test.bmp" width="150px" /> | 2% | <img src="./data/lamp-test_compressed_decompressed_02.bmp" width="150px" /> | Original size: 16MB <br> Compressed binary: 4.80MB |
|<img src="./data/lamp-test.bmp" width="150px" /> | 5% | <img src="./data/lamp-test_compressed_decompressed_05.bmp" width="150px" />| Original size: 16MB <br> Compressed binary: 12.00MB |
| <img src="./data/img-1024-s32H_1.bmp" width="150px" /> | 1% |<img src="./data/img-1024-s32H_1_compressed_decompressed.bmp" width="150px" /> | Original size: 12MB <br> Compressed  binary: 207KB |

| Initial Image | Decompressed image | Compression Rate |
|---------------|--------------------|------------------|
| <img src="./data/grayscale-lamp.png" width="150px" /> | <img src="./data/grayscale-lamp_dct_compressed_decompressed.bmp" width="150px"/> | Original size: 16MB <br> Compressed Binary: 357KB <br> <b> Ratio: 2% of original size</b> | 
| <img src="./data/lamp-test-256.bmp" width="150px" /> | <img src="./data/lamp-test-256_dct_compressed_decompressed.bmp" width="150px"/> | Original size: 257KB <br> Compressed Binary: 7KB <br> <b> Ratio: 2% of original size</b> |
| <img src="./data/sample_1024.bmp" width="150px" /> | <img src="./data/sample_1024_dct_compressed_decompressed.bmp" width="150px"/> | Original size: 4097KB <br> Compressed Binary: 134KB <br> <b> Ratio: 3% of original size</b> |
| <img src="./data/kitty-bw.jpg" width="150px" /> | <img src="./data/kitty_dct_compressed_decompressed.png" width="150px"/> | Original size: 12.289KB <br> Compressed Binary: 134KB <br> <b> Ratio: 1% of original size</b> |


### Changelog 
- implemented fft iterative version (for test image 30% faster)

## Image encryption
| Initial Image | Encrypted Image|
|---------------|----------------|
|<img src="./data/encryption-input.png" width="150px" /> | <img src="./data/encryption-output.png" width="150px" /> |


| Correctly Decrypted Image | Faulty Decrypted Image|
|---------------|----------------|
|<img src="./data/decryption-output.png" width="150px" /> | <img src="./data/faulty_decoded.png" width="150px" /> |



# Reference
1. [Optical image encryption using different twiddle factors in the butterfly algorithm of FFT](https://www.sciencedirect.com/science/article/pii/S0030401820311263?ref=pdf_download&fr=RR-2&rr=821ca33efbc0284e)
2. [FFT Based Compression Approach for Medical Images](https://www.ripublication.com/ijaer18/ijaerv13n6_54.pdf)
3. [Comparison methods of DCT, DWT and FFT techniques approach on lossy image compression](https://ieeexplore.ieee.org/stamp/stamp.jsp?arnumber=8308126)
4. [Efficient Fractal Image Coding using Fast Fourier Transform ](https://core.ac.uk/download/pdf/233149698.pdf)
5. [DCT efficient implementation based on FFT:](https://www.uio.no/studier/emner/matnat/math/nedlagte-emner/MAT-INF2360/v12/fft.pdf) 
