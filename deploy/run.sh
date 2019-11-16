gcloud iam service-accounts describe \
    terraform@arena-team.iam.gserviceaccount.com \
    --format='value(uniqueId)'
