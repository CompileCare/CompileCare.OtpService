# Navigate to the directory
cd CompileCare.OtpService\src\CompileCare.OtpService.Api

# Build and run
docker-compose up --build

# Or in detached mode
docker-compose up --build -d

# View logs
docker-compose logs -f otp-api