import React, { useCallback, useState } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditMetadataSourceModal from './EditMetadataSourceModal';
import styles from './MetadataSource.css';

interface MetadataSourceProps {
  id: number;
  name: string;
  enable: boolean;
  priority: number;
}

function MetadataSource({ id, name, enable, priority }: MetadataSourceProps) {
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  const handleOpenPress = useCallback(() => {
    setIsEditModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsEditModalOpen(false);
  }, []);

  return (
    <Card
      className={styles.metadataSource}
      overlayContent={true}
      onPress={handleOpenPress}
    >
      <div className={styles.name}>{name}</div>

      <div className={styles.priority}>
        {translate('Priority')}: {priority}
      </div>

      <div>
        {enable ? (
          <Label kind={kinds.SUCCESS}>{translate('Enabled')}</Label>
        ) : (
          <Label kind={kinds.DISABLED} outline={true}>
            {translate('Disabled')}
          </Label>
        )}
      </div>

      <EditMetadataSourceModal
        id={id}
        isOpen={isEditModalOpen}
        onModalClose={handleModalClose}
      />
    </Card>
  );
}

export default MetadataSource;
