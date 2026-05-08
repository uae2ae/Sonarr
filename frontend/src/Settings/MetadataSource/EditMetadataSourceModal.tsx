import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import EditMetadataSourceModalContent from './EditMetadataSourceModalContent';

interface EditMetadataSourceModalProps {
  id: number;
  isOpen: boolean;
  onModalClose: () => void;
}

function EditMetadataSourceModal({
  id,
  isOpen,
  onModalClose,
}: EditMetadataSourceModalProps) {
  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={onModalClose}>
      <EditMetadataSourceModalContent id={id} onModalClose={onModalClose} />
    </Modal>
  );
}

export default EditMetadataSourceModal;
